import glob
import random
import os
import numpy as np
import torch
from torch.utils.data import Dataset
import albumentations as A
from albumentations.pytorch import ToTensorV2
from PIL import Image
import matplotlib.pyplot as plt
import json
import itertools

def one_hot_encoding(floats_list):
    # Generate all possible orderings
    permutations = list(itertools.permutations(floats_list, len(floats_list)))
    # Sort permutations
    sorted_permutations = sorted(permutations, reverse=True)
    # Find out the index of the original order in the sorted list
    index = sorted_permutations.index(tuple(floats_list))
    # Create the one-hot encoding
    one_hot = [0] * len(sorted_permutations)
    one_hot[index] = 1
    return one_hot

def visualize_7channels(img: np.ndarray):
    """
    Visualize 7 channels of an image in a grid pattern.

    Parameters:
    - img: A numpy ndarray with shape (height, width, 7)
    """

    if img.shape[2] != 7:
        raise ValueError("The image should have 7 channels")

    fig, axarr = plt.subplots(3, 3, figsize=(10, 10))

    # List of channel indices
    channel_indices = list(range(7))

    # Iterate over 3x3 grid
    for i in range(3):
        for j in range(3):
            if channel_indices:
                channel = channel_indices.pop(0)
                axarr[i, j].imshow(img[:, :, channel], cmap='gray')
                axarr[i, j].set_title(f'Channel {channel+1}')
                axarr[i, j].axis('off')
            else:
                axarr[i, j].axis('off')

    plt.tight_layout()
    plt.show()

def read_json_labes(filename):
    with open(filename, 'r') as f:
        data = json.load(f)        
    processed_data = {}
    for key, value in data.items():
        processed_data[key] = (value['wg'], value['wgr'], value['wi'], value['wc'])    
    return processed_data

def get_filename_key(path):
    base_name = os.path.basename(path)
    return os.path.splitext(base_name)[0]

class ImageDataset(Dataset):
    def __init__(self, mode="train", start=0, end=1):
        self.mode = mode
        self.imgs = sorted(glob.glob(os.path.join("./PATH-TO-DATA/SyntheticData") + "/*.*"))[start:end]
        self.labels = read_json_labes("./PATH-TO-DATA/labels.json") #(goal, group, interaction, connection)

    def __getitem__(self, index):
        img_name = self.imgs[index % len(self.imgs)]
        img_temp = np.load(img_name)
        img_np = img_temp[list(img_temp.keys())[0]].astype(np.float32)

        if self.mode == "train":
            randomDegreeRange = random.choice([(80,100), (170,190), (260,280), (350,370)])
            current_transforms = A.Compose([
                A.Rotate(limit=randomDegreeRange, always_apply=True),
                A.HorizontalFlip(p=0.5),
                A.VerticalFlip(p=0.5),
                ToTensorV2()
            ])
        else:
            current_transforms = A.Compose([
                ToTensorV2()
            ])

        transformed = current_transforms(image=img_np)
        img = transformed['image']

        label_key = get_filename_key(img_name)
        label = self.labels[label_key]
        return img, torch.tensor(label, dtype=torch.float32)

    def __len__(self):
        return len(self.imgs)