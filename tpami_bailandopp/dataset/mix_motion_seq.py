# This source code is licensed under the MIT license found in the
# LICENSE file in the root directory of this open-source project.


""" Define the dance dataset. """
import numpy as np
import torch
import torch.utils.data
from torch.utils.data import Dataset
import pdb


def paired_collate_fn(insts):
    # for src in insts:
    #     for s in src:
    #         print(s.shape)

    # print()

    mo_seq = list(zip(*insts))
    print(mo_seq.shape)
    mo_seq = torch.FloatTensor(mo_seq)
    print('Here!')
    print(mo_seq.size())

    return mo_seq


class MixMoSeq(Dataset):
    def __init__(self, dances, dances2):
        self.dances = dances
        self.dances2 = dances2

    def __len__(self):
        return len(self.dances)

    def __getitem__(self, index):
        # print(self.dances[index].shape)
        return self.dances[index], self.dances2[index]
