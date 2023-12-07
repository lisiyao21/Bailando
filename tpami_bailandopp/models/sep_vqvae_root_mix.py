import numpy as np
import torch
import torch.nn as nn
import torch.nn.functional as F

from smplx import SMPL
from pytorch3d.transforms.rotation_conversions import matrix_to_axis_angle

# from models.vqvae_root_rotmat import VQVAER_rotmat

# from .encdec import Encoder, Decoder, assert_shape
# from .bottleneck import NoBottleneck, Bottleneck
# from .utils.logger import average_metrics
# from .utils.audio_utils import  audio_postprocess

from .vqvae_mix import VQVAEmix
from .vqvae_root_mix import VQVAERmix
import torch as t

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


def _loss_fn(x_target, x_pred):

    # if not weighted:
    return torch.mean(torch.abs(x_pred - x_target)) 
    # else:
    #     n, tt, c = x_target.size()
    #     # print(tt, flush=True)
    #     # assert(tt == 15)
    #     return torch.mean(torch.abs(x_pred.view(n, tt, 15, c//15) - x_target.view(n, tt, 15, c//15)) * torch.tensor(up_weighted).cuda().view(1, 1, 15, 1))



def rotation_6d_to_matrix(d6: torch.Tensor) -> torch.Tensor:
    """
    Converts 6D rotation representation by Zhou et al. [1] to rotation matrix
    using Gram--Schmidt orthogonalization per Section B of [1].
    Args:
        d6: 6D rotation representation, of size (*, 6)

    Returns:
        batch of rotation matrices of size (*, 3, 3)

    [1] Zhou, Y., Barnes, C., Lu, J., Yang, J., & Li, H.
    On the Continuity of Rotation Representations in Neural Networks.
    IEEE Conference on Computer Vision and Pattern Recognition, 2019.
    Retrieved from http://arxiv.org/abs/1812.07035
    """

    a1, a2 = d6[..., :3], d6[..., 3:]
    b1 = F.normalize(a1, dim=-1)
    b2 = a2 - (b1 * a2).sum(-1, keepdim=True) * b1
    b2 = F.normalize(b2, dim=-1)
    b3 = torch.cross(b1, b2, dim=-1)
    return torch.stack((b1, b2, b3), dim=-2)


def matrix_to_rotation_6d(matrix: torch.Tensor) -> torch.Tensor:
    """
    Converts rotation matrices to 6D rotation representation by Zhou et al. [1]
    by dropping the last row. Note that 6D representation is not unique.
    Args:
        matrix: batch of rotation matrices of size (*, 3, 3)

    Returns:
        6D rotation representation, of size (*, 6)

    [1] Zhou, Y., Barnes, C., Lu, J., Yang, J., & Li, H.
    On the Continuity of Rotation Representations in Neural Networks.
    IEEE Conference on Computer Vision and Pattern Recognition, 2019.
    Retrieved from http://arxiv.org/abs/1812.07035
    """
    batch_dim = matrix.size()[:-2]
    return matrix[..., :2, :].clone().reshape(batch_dim + (6,))



class SepVQVAERmix(nn.Module):
    def __init__(self, hps):
        super().__init__()
        self.hps = hps
        # self.cut_dim = hps.up_half_dim
        # self.use_rotmat = hps.use_rotmat if (hasattr(hps, 'use_rotmat') and hps.use_rotmat) else False
        self.chanel_num = hps.joint_channel
        self.chanel_num_rot = hps.rot_channel
        print(self.chanel_num_rot, flush=True)
        self.vqvae_up = VQVAEmix(hps.up_half, len(smpl_up)*self.chanel_num, len(smpl_up)*self.chanel_num_rot)
        self.vqvae_down = VQVAERmix(hps.down_half, len(smpl_down)*self.chanel_num, len(smpl_down)*self.chanel_num_rot)
        self.use_6d_rotation = hps.use_6d_rotation if hasattr(hps, 'use_6d_rotation') else False
        if self.use_6d_rotation:
            assert(self.chanel_num_rot == 6)
        
        self.smpl_weight = hps.smpl_weight if hasattr(hps, 'smpl_weight') else 0
        if self.smpl_weight > 0:
            self.smpl = SMPL(model_path='/mnt/lustre/syli/dance/Bailando/smpl', gender='MALE', batch_size=1).eval()
        # self.use_rotmat = hps.use_rotmat if (hasattr(hps, 'use_rotmat') and hps.use_rotmat) else False
        # self.chanel_num = 9 if self.use_rotmat else 3

    def matrix_to_smpl(self, matrix):
        n, t = matrix.size()[:2]
        matrix = matrix.view(n*t, 24, 3, 3)
        
        aa = matrix_to_axis_angle(matrix)

        pos3d = self.smpl.eval().forward(
            global_orient=aa[:, 0:1].float(),
            body_pose=aa[:, 1:].float(),
        ).joints[:, 0:24, :]

        return pos3d.view(n, t, 24, 3)

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
        xdown, xvel = self.vqvae_down.decode(zdown)
        b, t, cup = xup.size()
        _, _, cdown = xdown.size()
        x = torch.zeros(b, t, (cup+cdown)//self.chanel_num_rot, self.chanel_num_rot).cuda()
        x[:, :, smpl_up] = xup.view(b, t, cup//self.chanel_num_rot, self.chanel_num_rot)
        x[:, :, smpl_down] = xdown.view(b, t, cdown//self.chanel_num_rot, self.chanel_num_rot)

        if self.use_6d_rotation:
            x = rotation_6d_to_matrix(x)
        
        return torch.cat([xvel, x.view(b, t, -1)], dim=2)


        # z_chunks = [torch.chunk(z, bs_chunks, dim=0) for z in zs]
        # x_outs = []
        # for i in range(bs_chunks):
        #     zs_i = [z_chunk[i] for z_chunk in z_chunks]
        #     x_out = self._decode(zs_i, start_level=start_level, end_level=end_level)
        #     x_outs.append(x_out)

        # return torch.cat(x_outs, dim=0)

    def encode(self, x, start_level=0, end_level=None, bs_chunks=1):
        x[:, :, :3] = 0
        b, t, c = x.size()
        zup = self.vqvae_up.encode(x.view(b, t, c//self.chanel_num, self.chanel_num)[:, :, smpl_up].view(b, t, -1), start_level, end_level, bs_chunks)
        zdown = self.vqvae_down.encode(x.view(b, t, c//self.chanel_num, self.chanel_num)[:, :, smpl_down].view(b, t, -1), start_level, end_level, bs_chunks)
        return (zup, zdown)

    def sample(self, n_samples):
        # zs = [torch.randint(0, self.l_bins, size=(n_samples, *z_shape), device='cuda') for z_shape in self.z_shapes]
        xup = self.vqvae_up.sample(n_samples)
        xdown, xvel = self.vqvae_down.sample(n_samples)
        b, t, cup = xup.size()
        _, _, cdown = xdown.size()
        x = torch.zeros(b, t, (cup+cdown)//self.chanel_num_rot, self.chanel_num_rot).cuda()
        x[:, :, smpl_up] = xup.view(b, t, cup//self.chanel_num_rot, self.chanel_num_rot)
        x[:, :, smpl_down] = xdown.view(b, t, cdown//self.chanel_num_rot, self.chanel_num_rot)

        return torch.cat([xvel, xdown.view(b, t, -1)], dim=2)

    def forward(self, x, x_rot):
        b, t, c = x.size()

        x_vel = x_rot[:, :, :3]
        x[:, :, :3] = 0
        x_rot = x_rot[:, :, 3:]

        x = x.view(b, t, c//self.chanel_num, self.chanel_num)
        xup = x[:, :, smpl_up, :].view(b, t, -1)
        xdown = x[:, :, smpl_down, :].view(b, t, -1)

        b, t, c = x_rot.size()
        
        x_rot = x_rot.view(b, t, c//9, 3, 3)
        x_rot33 = x_rot.view(b, t, c//9, 3, 3).clone().detach()
        if self.use_6d_rotation:
            # x_rot33 = x_rot.view(b, t, c//9, 3, 3).clone().detach()
            x_rot = matrix_to_rotation_6d(x_rot)
    
        xup_rot = x_rot[:, :, smpl_up, :].view(b, t, -1)
        xdown_rot = x_rot[:, :, smpl_down, :].view(b, t, -1)

        # self.vqvae_up.eval()
        x_out_up, loss_up, metrics_up = self.vqvae_up(xup, xup_rot)
        x_out_down, x_out_vel, loss_down , metrics_down  = self.vqvae_down(xdown, xdown_rot, x_vel)

        _, _, cup = x_out_up.size()
        _, _, cdown = x_out_down.size()

        xout = torch.zeros(b, t, (cup+cdown)//self.chanel_num_rot, self.chanel_num_rot).cuda().float()
        xout[:, :, smpl_up] = xout[:, :, smpl_up] + x_out_up.view(b, t, cup//self.chanel_num_rot, self.chanel_num_rot)
        xout[:, :, smpl_down] = xout[:, :, smpl_down] + x_out_down.view(b, t, cdown//self.chanel_num_rot, self.chanel_num_rot)

        if self.use_6d_rotation:
            xout_mat = rotation_6d_to_matrix(xout)
        else:
            xout_mat = xout
        
        loss = (loss_up + loss_down)*0.5
        if self.smpl_weight > 0:
            self.smpl.eval()
            with torch.no_grad():
                pos3d_gt = self.matrix_to_smpl(x_rot33)
            pos3d = self.matrix_to_smpl(xout_mat)
            loss += self.smpl_weight * _loss_fn(pos3d_gt, pos3d)
        # loss += 10 * _loss_fn(x_rot[:, :, :1], xout[:, :, :1])
            
        xout_mat = torch.cat([x_out_vel, xout_mat.view(b, t , -1)], dim=2)
        
        # self.smpl_weight = hps.smpl_weight
        # if hps.smpl_weight > 0:
        #     self.smpl1 = SMPL(model_path='/mnt/lustre/syli/dance/Bailando/smpl', gender='MALE', batch_size=1).eval()

        # xout[:, :, smpl_up] = xup.view(b, t, cup//self.chanel_num, self.chanel_num).float()
        # xout[:, :, smpl_down] = xdown.view(b, t, cdown//self.chanel_num, self.chanel_num).float()
        # metrics_up['acceleration_loss'] *= 0
        # metrics_up['velocity_loss'] *= 0
        return xout_mat.view(b, t, -1), loss, [metrics_up, metrics_down] 
