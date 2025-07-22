# IMPORTS
import numpy as np
import torch.nn as nn
import torch
import torch.nn.init as init

class LossImportanceAdjuster:
    def __init__(self, initial_importance, step_size, increase_interval):
        self.conn_importance = initial_importance
        self.step_size = step_size
        self.triggered = False
        self.increase_interval = increase_interval
        self.counter = 0

    def step(self, accuracy, threshold):
        if accuracy > threshold:
            self.triggered = True
        if self.triggered:
            if self.counter % self.increase_interval == 0:
                self.conn_importance += self.step_size
                self.conn_importance = np.clip(self.conn_importance, 0, 1)
                print("Connectivity Weight increased: ", self.conn_importance)
            self.counter += 1

    def get_importance(self):
        return self.conn_importance

    def get_importance(self):
        return self.conn_importance

class CNN_Class_Reg(nn.Module):
    def __init__(self):
        super(CNN_Class_Reg, self).__init__()

        # Convolutional layers
        self.conv1 = nn.Sequential(
            nn.Conv2d(5, 32, kernel_size=3, padding=1, stride=1, bias=False),
            nn.LeakyReLU(inplace=True),
            nn.Dropout(0.2), # Added dropout here
            nn.MaxPool2d(2, 2)  # Pooling here
        )
        init.kaiming_normal_(self.conv1[0].weight, nonlinearity='leaky_relu')

        self.conv2 = nn.Sequential(
            nn.Conv2d(32, 64, kernel_size=3, padding=1, stride=1, bias=False),
            nn.LeakyReLU(inplace=True),
            nn.Dropout(0.2), # Added dropout here
            nn.MaxPool2d(2, 2)  # Pooling here
        )
        init.kaiming_normal_(self.conv2[0].weight, nonlinearity='leaky_relu')
        
        self.conv3 = nn.Sequential(
            nn.Conv2d(64, 128, kernel_size=3, padding=1, stride=1, bias=False),
            nn.LeakyReLU(inplace=True),
            nn.Dropout(0.2), # Added dropout here
            nn.MaxPool2d(2, 2)  # Pooling here
        )
        init.kaiming_normal_(self.conv3[0].weight, nonlinearity='leaky_relu')

        # Fully connected layers
        self.fc1 = nn.Sequential(
            nn.Linear(128 * 8 * 8, 2048, bias=False),
            nn.LeakyReLU(inplace=True),
            nn.Dropout(0.5) # Added dropout here
        )
        init.kaiming_normal_(self.fc1[0].weight, nonlinearity='leaky_relu')
        
        self.fc2 = nn.Sequential(
            nn.Linear(2048, 1024, bias=False),
            nn.LeakyReLU(inplace=True),
            nn.Dropout(0.5) # Added dropout here
        )
        init.kaiming_normal_(self.fc2[0].weight, nonlinearity='leaky_relu')
        
        self.fc3 = nn.Sequential(
            nn.Linear(1024, 256, bias=False),
            nn.LeakyReLU(inplace=True),
            nn.Dropout(0.5) # Added dropout here
        )
        init.kaiming_normal_(self.fc3[0].weight, nonlinearity='leaky_relu')
        
        self.fc4 = nn.Linear(256, 6, bias=False) 
        init.kaiming_normal_(self.fc4.weight, nonlinearity='leaky_relu')

    def forward(self, x):
        x = self.conv1(x)
        x = self.conv2(x)
        x = self.conv3(x)
        x = x.view(-1, 128 * 8 * 8)
        x = self.fc1(x)
        x = self.fc2(x)
        x = self.fc3(x)
        x = self.fc4(x)

        x_diff = torch.sigmoid(x[:, 3:6])
        return torch.cat((x[:, 0:3], x_diff), dim=1)