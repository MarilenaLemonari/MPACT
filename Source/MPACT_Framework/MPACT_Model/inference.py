import os
import numpy as np
import torch
from Models.model import CNN_Class_Reg

# Set device
device = torch.device('cuda')
torch.backends.cudnn.benchmark = True
scaler = torch.cuda.amp.GradScaler()

# Initialize the models
model = CNN_Class_Reg().to(device)
current_file_dir = os.path.dirname(os.path.abspath(__file__))
saved_model_path = current_file_dir + '\mpact_model.pt'
checkpoint = torch.load(saved_model_path)
model.load_state_dict(checkpoint['model_state_dict'])

def format_profile(p):
    if p is None: return None
    
    a, b, c, d = p
    min_val = min(a, b, c)
    
    a += abs(min_val)
    b += abs(min_val)
    c += abs(min_val)

    total = a + b + c
    a /= total
    b /= total
    c /= total

    a = round(a, 4)
    b = round(b, 4)
    c = round(c, 4)
    d = round(d, 4)
    
    return (a, b, c, d)

def predict(images, masking):
    stacked_images = np.stack(images, axis=0)
    inputs = torch.from_numpy(stacked_images)
    inputs = inputs.permute(0, 3, 1, 2)

    with torch.no_grad():
        model.eval()
        inputs = inputs.contiguous().to(device)

        # Use autocast for the forward pass
        with torch.cuda.amp.autocast():
            reg_outputs = model(inputs).float()

            v1 = reg_outputs[:,3]
            v2 = reg_outputs[:,4]
            values, indices = torch.topk(reg_outputs[:, 0:3], 3, dim=1)            
            pred_first = indices[:, 0]
            pred_second = indices[:, 1]

            # Initialize tensors for w_a, w_b, and w_c
            w_a = torch.zeros_like(v1)
            w_b = torch.zeros_like(v1)
            w_c = torch.zeros_like(v1)

            # Compute the base value for w_a
            w_a_base = (1 + v1 + v2) / 3

            # 1. Order: A, B, C
            mask_abc = (pred_first == 0) & (pred_second == 1)
            w_a[mask_abc] = w_a_base[mask_abc]
            w_b[mask_abc] = w_a_base[mask_abc] - v1[mask_abc]
            w_c[mask_abc] = w_a_base[mask_abc] - v2[mask_abc]

            # 2. Order: A, C, B
            mask_acb = (pred_first == 0) & (pred_second == 2)
            w_a[mask_acb] = w_a_base[mask_acb]
            w_c[mask_acb] = w_a_base[mask_acb] - v1[mask_acb]
            w_b[mask_acb] = w_a_base[mask_acb] - v2[mask_acb]

            # 3. Order: B, A, C
            mask_bac = (pred_first == 1) & (pred_second == 0)
            w_b[mask_bac] = w_a_base[mask_bac]
            w_a[mask_bac] = w_a_base[mask_bac] - v1[mask_bac]
            w_c[mask_bac] = w_a_base[mask_bac] - v2[mask_bac]

            # 4. Order: B, C, A
            mask_bca = (pred_first == 1) & (pred_second == 2)
            w_b[mask_bca] = w_a_base[mask_bca]
            w_c[mask_bca] = w_a_base[mask_bca] - v1[mask_bca]
            w_a[mask_bca] = w_a_base[mask_bca] - v2[mask_bca]

            # 5. Order: C, A, B
            mask_cab = (pred_first == 2) & (pred_second == 0)
            w_c[mask_cab] = w_a_base[mask_cab]
            w_a[mask_cab] = w_a_base[mask_cab] - v1[mask_cab]
            w_b[mask_cab] = w_a_base[mask_cab] - v2[mask_cab]

            # 6. Order: C, B, A
            mask_cba = (pred_first == 2) & (pred_second == 1)
            w_c[mask_cba] = w_a_base[mask_cba]
            w_b[mask_cba] = w_a_base[mask_cba] - v1[mask_cba]
            w_a[mask_cba] = w_a_base[mask_cba] - v2[mask_cba]

            w_conn = reg_outputs[:, 5]
            predicted_weights = torch.stack([w_a, w_b, w_c, w_conn], dim=1)     

            profiles = [tuple(row.tolist()) if mask == 1 else None for row, mask in zip(predicted_weights, masking)]
            profiles = [format_profile(p) for p in profiles]
            return profiles