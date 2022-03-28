import numpy as np
import torch
import torch.nn as nn

# from .encdec import Encoder, Decoder, assert_shape
# from .bottleneck import NoBottleneck, Bottleneck
# from .utils.logger import average_metrics
# from .utils.audio_utils import  audio_postprocess

from .vqvae import VQVAE
from .vqvae_root import VQVAER


smpl_down = [0, 1, 2, 4,  5, 7, 8, 10, 11]
smpl_up = [3, 6, 9, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23]

# def dont_update(params):
#     for param in params:
#         param.requires_grad = False

# def update(params):
#     for param in params:
#         param.requires_grad = True

# def calculate_strides(strides, downs):
#     return [stride ** down for stride, down in zip(strides, downs)]

# # def _loss_fn(loss_fn, x_target, x_pred, hps):
#     if loss_fn == 'l1':
#         return torch.mean(torch.abs(x_pred - x_target)) / hps.bandwidth['l1']
#     elif loss_fn == 'l2':
#         return torch.mean((x_pred - x_target) ** 2) / hps.bandwidth['l2']
#     elif loss_fn == 'linf':
#         residual = ((x_pred - x_target) ** 2).reshape(x_targetorch.shape[0], -1)
#         values, _ = torch.topk(residual, hps.linf_k, dim=1)
#         return torch.mean(values) / hps.bandwidth['l2']
#     elif loss_fn == 'lmix':
#         loss = 0.0
#         if hps.lmix_l1:
#             loss += hps.lmix_l1 * _loss_fn('l1', x_target, x_pred, hps)
#         if hps.lmix_l2:
#             loss += hps.lmix_l2 * _loss_fn('l2', x_target, x_pred, hps)
#         if hps.lmix_linf:
#             loss += hps.lmix_linf * _loss_fn('linf', x_target, x_pred, hps)
#         return loss
#     else:
#         assert False, f"Unknown loss_fn {loss_fn}"
# def _loss_fn(x_target, x_pred):
#     return torch.mean(torch.abs(x_pred - x_target)) 


class SepVQVAER(nn.Module):
    def __init__(self, hps):
        super().__init__()
        self.hps = hps
        # self.cut_dim = hps.up_half_dim
        # self.use_rotmat = hps.use_rotmat if (hasattr(hps, 'use_rotmat') and hps.use_rotmat) else False
        self.chanel_num = hps.joint_channel
        self.vqvae_up = VQVAE(hps.up_half, len(smpl_up)*self.chanel_num)
        self.vqvae_down = VQVAER(hps.down_half, len(smpl_down)*self.chanel_num)
        # self.use_rotmat = hps.use_rotmat if (hasattr(hps, 'use_rotmat') and hps.use_rotmat) else False
        # self.chanel_num = 9 if self.use_rotmat else 3


    def decode(self, zs, start_level=0, end_level=None, bs_chunks=1):
        """
        zs are list with two elements: z for up and z for down
        """
        if isinstance(zs, tuple):
            zup = zs[0]
            zdown = zs[1]
        else:
            zup = zs
            zdown = zs
        xup = self.vqvae_up.decode(zup)
        xdown = self.vqvae_down.decode(zdown)
        b, t, cup = xup.size()
        _, _, cdown = xdown.size()
        x = torch.zeros(b, t, (cup+cdown)//self.chanel_num, self.chanel_num).cuda()
        x[:, :, smpl_up] = xup.view(b, t, cup//self.chanel_num, self.chanel_num)
        x[:, :, smpl_down] = xdown.view(b, t, cdown//self.chanel_num, self.chanel_num)
        
        return x.view(b, t, -1)


        # z_chunks = [torch.chunk(z, bs_chunks, dim=0) for z in zs]
        # x_outs = []
        # for i in range(bs_chunks):
        #     zs_i = [z_chunk[i] for z_chunk in z_chunks]
        #     x_out = self._decode(zs_i, start_level=start_level, end_level=end_level)
        #     x_outs.append(x_out)

        # return torch.cat(x_outs, dim=0)

    def encode(self, x, start_level=0, end_level=None, bs_chunks=1):
        b, t, c = x.size()
        zup = self.vqvae_up.encode(x.view(b, t, c//self.chanel_num, self.chanel_num)[:, :, smpl_up].view(b, t, -1), start_level, end_level, bs_chunks)
        zdown = self.vqvae_down.encode(x.view(b, t, c//self.chanel_num, self.chanel_num)[:, :, smpl_down].view(b, t, -1), start_level, end_level, bs_chunks)
        return (zup, zdown)

    def sample(self, n_samples):
        # zs = [torch.randint(0, self.l_bins, size=(n_samples, *z_shape), device='cuda') for z_shape in self.z_shapes]
        xup = self.vqvae_up.sample(n_samples)
        xdown = self.vqvae_up.sample(n_samples)
        b, t, cup = xup.size()
        _, _, cdown = xdown.size()
        x = torch.zeros(b, t, (cup+cdown)//self.chanel_num, self.chanel_num).cuda()
        x[:, :, smpl_up] = xup.view(b, t, cup//self.chanel_num, self.chanel_num)
        x[:, :, smpl_down] = xdown.view(b, t, cdown//self.chanel_num, self.chanel_num)
        return x

    def forward(self, x):
        b, t, c = x.size()
        x = x.view(b, t, c//self.chanel_num, self.chanel_num)
        xup = x[:, :, smpl_up, :].view(b, t, -1)
        xdown = x[:, :, smpl_down, :].view(b, t, -1)
        # xup[:] = 0
        
        self.vqvae_up.eval()
        x_out_up, loss_up, metrics_up = self.vqvae_up(xup)
        x_out_down , loss_down , metrics_down  = self.vqvae_down(xdown)

        _, _, cup = x_out_up.size()
        _, _, cdown = x_out_down.size()

        xout = torch.zeros(b, t, (cup+cdown)//self.chanel_num, self.chanel_num).cuda().float()
        xout[:, :, smpl_up] = x_out_up.view(b, t, cup//self.chanel_num, self.chanel_num)
        xout[:, :, smpl_down] = x_out_down.view(b, t, cdown//self.chanel_num, self.chanel_num)
        
        # xout[:, :, smpl_up] = xup.view(b, t, cup//self.chanel_num, self.chanel_num).float()
        # xout[:, :, smpl_down] = xdown.view(b, t, cdown//self.chanel_num, self.chanel_num).float()
        metrics_up['acceleration_loss'] *= 0
        metrics_up['velocity_loss'] *= 0
        return xout.view(b, t, -1), loss_down, [metrics_up, metrics_down] 
