import os
import time
import random
from tqdm.auto import tqdm
import torch
import torch.nn as nn
import torch.optim as optim
from torch.optim.lr_scheduler import ReduceLROnPlateau
from torch.utils.data import DataLoader
from dataloader import *
from model import *
from utils import *

def worker_init_fn(worker_id):
    worker_seed = (torch.initial_seed() + worker_id) % 2**32
    np.random.seed(worker_seed)
    random.seed(worker_seed)

if __name__ ==  '__main__':
    os.makedirs("output/saved_models/", exist_ok=True)
    os.makedirs("output/saved_info/", exist_ok=True)

    resume_train = False

    # Set hyperparameters
    learning_rate = 0.0005
    batch_size = 256
    num_epochs = 5000
    lambda_l1 = 0.0001
    weight_decay = 0.0001
    start_epoch = 0
    tolerance = 0.1
    importance_second = 0.3

    # Set device
    device = torch.device('cuda')
    torch.backends.cudnn.benchmark = True
    scaler = torch.cuda.amp.GradScaler()

    # Initialize the models
    model = CNN_Class_Reg().to(device)

    # Define the loss function and optimizer
    criterionDominant = nn.CrossEntropyLoss()
    criterionReg = nn.L1Loss()
    optimizer = optim.AdamW(model.parameters(), lr=learning_rate, weight_decay=weight_decay)
    scheduler = ReduceLROnPlateau(optimizer, mode='min', factor=0.9, patience=10, verbose=True)

    if resume_train:
        checkpoint = torch.load('output/saved_models/model_epoch_25.pt')
        # Load the saved model and optimizer states
        model.load_state_dict(checkpoint['model_state_dict'])
        optimizer.load_state_dict(checkpoint['optimizer_reg_state_dict'])
        scheduler.load_state_dict(checkpoint['scheduler_reg_state_dict'])
        start_epoch = 26
    else:
        params = (f"LR_AC: {learning_rate}, BS: {batch_size}, L1: {lambda_l1}, WD: {weight_decay}")
        with open("output/saved_info/info.txt", "a") as f:
                f.write(params + "\n")

    # Data augmentation and normalization for training
    train_loader = DataLoader(
        ImageDataset(mode="train", start=0, end=112500),
        batch_size=batch_size,
        shuffle=True,
        num_workers=4,
        pin_memory=True,
        worker_init_fn=worker_init_fn
    )
    val_loader = DataLoader(
        ImageDataset(mode="val", start=112500, end=150153),
        batch_size=batch_size,
        shuffle=False,
        num_workers=4,
        pin_memory=True,
        worker_init_fn=worker_init_fn
    )

    start_time = time.time()

    for epoch in tqdm(range(start_epoch, num_epochs), position=0, leave=True, desc="Epochs"):
        model.train()
        running_loss = 0.0
        for inputs, ground_truth_weights in tqdm(train_loader, position=1, leave=False, desc="Training " + str(epoch)):
            inputs, ground_truth_weights = inputs.to(device, non_blocking=True), ground_truth_weights.to(device, non_blocking=True)

            # Train the regression model
            optimizer.zero_grad(set_to_none=True)

            # Use autocast for the forward pass
            with torch.cuda.amp.autocast():
                reg_outputs = model(inputs) #[0,0,0][0.4, 0.6, 0.8]

                truth_class = torch.argmax(ground_truth_weights[:, 0:3], 1)
                top2_values, top2_indices = torch.topk(ground_truth_weights[:, 0:3], 2, dim=1)
                second_class = top2_indices[:, 1]

                class_loss = criterionDominant(reg_outputs[:,0:3], truth_class)
                class_loss_2 = criterionDominant(reg_outputs[:,0:3], second_class)

                v1 = reg_outputs[:, 3]
                v2 = reg_outputs[:, 4]

                values, indices = torch.topk(reg_outputs[:, 0:3], 3, dim=1)
                pred_first = indices[:, 0]
                pred_second = indices[:, 1]
                pred_third = indices[:, 2]

                w_a = (1 + v1 + v2) / 3.0
                w_b = w_a - v1
                w_c = w_a - v2
                w_conn = reg_outputs[:, 5]
                predicted_weights = torch.cat((w_a.unsqueeze(1), w_b.unsqueeze(1), w_c.unsqueeze(1)), dim=1)

                current_batch_size = ground_truth_weights.size(0)
                reg_loss_a = criterionReg(w_a, ground_truth_weights[:, 0:3][torch.arange(current_batch_size), pred_first])
                reg_loss_b = criterionReg(w_b, ground_truth_weights[:, 0:3][torch.arange(current_batch_size), pred_second])
                reg_loss_c = criterionReg(w_c, ground_truth_weights[:, 0:3][torch.arange(current_batch_size), pred_third])
                reg_loss_conn = criterionReg(w_conn, ground_truth_weights[:, 3])
                total_loss = ((1 - importance_second) * class_loss + importance_second * class_loss_2) + reg_loss_a + reg_loss_b + reg_loss_c + reg_loss_conn

            # Use the scaler for the backward pass
            scaler.scale(total_loss).backward()
            scaler.step(optimizer)
            scaler.update()

            running_loss += total_loss.item()
            
        running_loss /= len(train_loader)

        # Print the average loss for the epoch
        out_train = ('[Epoch %d] Train_Loss: %.3f,' % (epoch + 1, running_loss))

        # Evaluate the model on the validation set
        correct_total = 0
        correct_second = 0
        correct_goal = 0
        correct_group = 0
        correct_interact = 0
        correct_connect = 0
        total = 0
        total_val_loss = 0.0
        with torch.no_grad():
            model.eval()
            for inputs, ground_truth_weights in tqdm(val_loader, position=1, leave=False, desc="Validation " + str(epoch)):
                inputs, ground_truth_weights = inputs.to(device, non_blocking=True), ground_truth_weights.to(device, non_blocking=True)
                
                # Use autocast for the forward pass
                with torch.cuda.amp.autocast():
                    reg_outputs = model(inputs) #[0,0,0][0.4, 0.6]
                    
                    truth_class = torch.argmax(ground_truth_weights[:, 0:3], 1)
                    top2_values, top2_indices = torch.topk(ground_truth_weights[:, 0:3], 2, dim=1)
                    second_class = top2_indices[:, 1]

                    class_loss = criterionDominant(reg_outputs[:,0:3], truth_class)
                    class_loss_2 = criterionDominant(reg_outputs[:,0:3], second_class)

                    v1 = reg_outputs[:, 3]
                    v2 = reg_outputs[:, 4]

                    values, indices = torch.topk(reg_outputs[:, 0:3], 3, dim=1)
                    pred_first = indices[:, 0]
                    pred_second = indices[:, 1]
                    pred_third = indices[:, 2]

                    w_a = (1 + v1 + v2) / 3.0
                    w_b = w_a - v1
                    w_c = w_a - v2
                    w_conn = reg_outputs[:, 5]
                    predicted_weights = torch.cat((w_a.unsqueeze(1), w_b.unsqueeze(1), w_c.unsqueeze(1)), dim=1)
                    
                    current_batch_size = ground_truth_weights.size(0)

                    reg_loss_a = criterionReg(w_a, ground_truth_weights[:,0:3][torch.arange(current_batch_size), pred_first])
                    reg_loss_b = criterionReg(w_b, ground_truth_weights[:,0:3][torch.arange(current_batch_size), pred_second])
                    reg_loss_c = criterionReg(w_c, ground_truth_weights[:,0:3][torch.arange(current_batch_size), pred_third])
                    reg_loss_conn = criterionReg(w_conn, ground_truth_weights[:, 3])
                    total_val_loss += (((1 - importance_second) * class_loss + importance_second * class_loss_2) + reg_loss_a + reg_loss_b + reg_loss_c + reg_loss_conn)

                predicted = torch.argmax(reg_outputs[:,0:3].data, 1)
                correct_total += (predicted == truth_class).sum().item()
                
                top2_values, top2_indices = torch.topk(reg_outputs[:,0:3], 2, dim=1)
                predicted_second = top2_indices[:, 1]
                correct_second += (predicted_second == second_class).sum().item()

                total += ground_truth_weights.size(0)
                # Goal
                diff = torch.abs(w_a - ground_truth_weights[torch.arange(current_batch_size), pred_first])
                correct_goal += (diff <= tolerance).sum().item()
                # Group
                diff = torch.abs(w_b - ground_truth_weights[torch.arange(current_batch_size), pred_second])
                correct_group += (diff <= tolerance).sum().item()
                # Interact
                diff = torch.abs(w_c - ground_truth_weights[torch.arange(current_batch_size), pred_third])
                correct_interact += (diff <= tolerance).sum().item()
                # Connect
                diff = torch.abs(w_conn - ground_truth_weights[:,3])
                correct_connect += (diff <= tolerance).sum().item()

        val_epoch_acc = correct_total / total
        val_second_acc = correct_second / total
        goal_acc = correct_goal / total
        group_acc = correct_group / total
        interact_acc = correct_interact / total
        connect_acc = correct_connect / total
        total_val_loss /= len(val_loader)

        # Update the learning rate
        scheduler.step(total_val_loss)

        out_val = ('Val_Loss: %.3f, First_Acc: %.3f, Second_Acc: %.3f, A_Acc: %.3f, B_Acc: %.3f, C_Acc: %.3f, Connect_Acc: %.3f, Time(min): %.3f' 
        % (total_val_loss, val_epoch_acc, val_second_acc, goal_acc, group_acc, interact_acc, connect_acc, ((time.time()-start_time)/60)))
        print("\n" + out_train, out_val)

        with open("output/saved_info/info.txt", "a") as f:
            f.write(out_train + " " + out_val + "\n")
        save_plots_all("output/saved_info/")

        # Save the model every N epochs
        if (epoch+1) % 25 == 0:
            torch.save({
                'epoch': epoch+1,
                'model_state_dict': model.state_dict(),
                'optimizer_reg_state_dict': optimizer.state_dict(),
                'scheduler_reg_state_dict': scheduler.state_dict(),
                'accuracy': val_epoch_acc,
                'loss': total_val_loss
            }, f'output/saved_models/model_epoch_{epoch+1}.pt')



