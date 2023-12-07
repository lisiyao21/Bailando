
from dataset.load_data import load_train_data_aist, load_test_data_aist
""" Define the paired music-dance dataset. """
import numpy as np
import torch
import torch.utils.data
from torch.utils.data import Dataset



class MusicDanceDataset(Dataset):
    def __init__(self, cfg):
        self.cfg = cfg
        self.dances = None
        self.musics = None
        self.mode = cfg.mode
        self._init_load()

    def _init_load(self):
        if self.mode == 'train':
            musics, dances, fnames = load_train_data_aist(self.cfg)
        elif self.mode == 'test':
            musics, dances, fnames = load_test_data_aist(self.cfg)
        
        self.musics = musics
        self.dances = dances
        self.fnames = fnames

    def __len__(self):
        return len(self.musics)

    def __getitem__(self, index):
        if self.dances is not None:
            return self.musics[index], self.dances[index],
        else:
            return self.musics[index]

