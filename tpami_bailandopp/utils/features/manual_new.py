# BSD License

# For fairmotion software

# Copyright (c) Facebook, Inc. and its affiliates. All rights reserved.
# Modified by Ruilong Li

# Redistribution and use in source and binary forms, with or without modification,
# are permitted provided that the following conditions are met:

#  * Redistributions of source code must retain the above copyright notice, this
#    list of conditions and the following disclaimer.

#  * Redistributions in binary form must reproduce the above copyright notice,
#    this list of conditions and the following disclaimer in the documentation
#    and/or other materials provided with the distribution.

#  * Neither the name Facebook nor the names of its contributors may be used to
#    endorse or promote products derived from this software without specific
#    prior written permission.

# THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
# ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
# WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
# DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
# ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
# (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
# LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
# ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
# (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
# SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
import numpy as np
from . import utils as feat_utils


SMPL_JOINT_NAMES = [
    "root",     
    "lhip", "rhip", "belly",    
    "lknee", "rknee", "spine",    
    "lankle", "rankle", "chest",     
    "ltoes", "rtoes", "neck", 
    "linshoulder", "rinshoulder",     
    "head",  "lshoulder", "rshoulder",      
    "lelbow", "relbow",      
    "lwrist", "rwrist",     
    "lhand", "rhand",
]


def extract_manual_features(positions):
    assert len(positions.shape) == 3  # (seq_len, n_joints, 3) 
    features = []
    f = ManualFeatures(positions)
    for _ in range(1, positions.shape[0]):
        pose_features = []
        pose_features.append(
            f.f_nmove("neck", "rhip", "lhip", "rwrist", 1.8 * f.hl)
        )
        pose_features.append(
            f.f_nmove("neck", "lhip", "rhip", "lwrist", 1.8 * f.hl)
        )
        pose_features.append(
            f.f_nplane("chest", "neck", "neck", "rwrist", 0.2 * f.hl)
        )
        pose_features.append(
            f.f_nplane("chest", "neck", "neck", "lwrist", 0.2 * f.hl)
        )
        pose_features.append(
            f.f_move("belly", "chest", "chest", "rwrist", 1.8 * f.hl)
        )
        pose_features.append(
            f.f_move("belly", "chest", "chest", "lwrist", 1.8 * f.hl)
        )
        pose_features.append(
            f.f_angle("relbow", "rshoulder", "relbow", "rwrist", [0, 110])
        )
        pose_features.append(
            f.f_angle("lelbow", "lshoulder", "lelbow", "lwrist", [0, 110])
        )
        pose_features.append(
            f.f_nplane(
                "lshoulder", "rshoulder", "lwrist", "rwrist", 2.5 * f.sw
            )
        )
        pose_features.append(
            f.f_move("lwrist", "rwrist", "rwrist", "lwrist", 1.4 * f.hl)
        )
        pose_features.append(
            f.f_move("rwrist", "root", "lwrist", "root", 1.4 * f.hl)
        )
        pose_features.append(
            f.f_move("lwrist", "root", "rwrist", "root", 1.4 * f.hl)
        )
        pose_features.append(f.f_fast("rwrist", 2.5 * f.hl))
        pose_features.append(f.f_fast("lwrist", 2.5 * f.hl))
        pose_features.append(
            f.f_plane("root", "lhip", "ltoes", "rankle", 0.38 * f.hl)
        )
        pose_features.append(
            f.f_plane("root", "rhip", "rtoes", "lankle", 0.38 * f.hl)
        )
        pose_features.append(
            f.f_nplane("zero", "y_unit", "y_min", "rankle", 1.2 * f.hl)
        )
        pose_features.append(
            f.f_nplane("zero", "y_unit", "y_min", "lankle", 1.2 * f.hl)
        )
        pose_features.append(
            f.f_nplane("lhip", "rhip", "lankle", "rankle", 2.1 * f.hw)
        )
        pose_features.append(
            f.f_angle("rknee", "rhip", "rknee", "rankle", [0, 110])
        )
        pose_features.append(
            f.f_angle("lknee", "lhip", "lknee", "lankle", [0, 110])
        )
        pose_features.append(f.f_fast("rankle", 2.5 * f.hl))
        pose_features.append(f.f_fast("lankle", 2.5 * f.hl))
        pose_features.append(
            f.f_angle("neck", "root", "rshoulder", "relbow", [25, 180])
        )
        pose_features.append(
            f.f_angle("neck", "root", "lshoulder", "lelbow", [25, 180])
        )
        pose_features.append(
            f.f_angle("neck", "root", "rhip", "rknee", [50, 180])
        )
        pose_features.append(
            f.f_angle("neck", "root", "lhip", "lknee", [50, 180])
        )
        pose_features.append(
            f.f_plane("rankle", "neck", "lankle", "root", 0.5 * f.hl)
        )
        pose_features.append(
            f.f_angle("neck", "root", "zero", "y_unit", [70, 110])
        )
        pose_features.append(
            f.f_nplane("zero", "minus_y_unit", "y_min", "rwrist", -1.2 * f.hl)
        )
        pose_features.append(
            f.f_nplane("zero", "minus_y_unit", "y_min", "lwrist", -1.2 * f.hl)
        )
        pose_features.append(f.f_fast("root", 2.3 * f.hl))
        features.append(pose_features)
        f.next_frame()
    features = np.array(features, dtype=np.float32).mean(axis=0)
    return features


class ManualFeatures:
    def __init__(self, positions, joint_names=SMPL_JOINT_NAMES):
        self.positions = positions
        self.joint_names = joint_names
        self.frame_num = 1

        # humerus length
        self.hl = feat_utils.distance_between_points(
            [1.99113488e-01,  2.36807942e-01, -1.80702247e-02],  # "lshoulder",
            [4.54445392e-01,  2.21158922e-01, -4.10167128e-02],  # "lelbow"
        )
        # shoulder width
        self.sw = feat_utils.distance_between_points(
            [1.99113488e-01,  2.36807942e-01, -1.80702247e-02],  # "lshoulder"
            [-1.91692337e-01,  2.36928746e-01, -1.23055102e-02,],  # "rshoulder"
        )
        # hip width
        self.hw = feat_utils.distance_between_points(
            [5.64076714e-02, -3.23069185e-01,  1.09197125e-02],  # "lhip"
            [-6.24834076e-02, -3.31302464e-01,  1.50412619e-02],  # "rhip"
        )

    def next_frame(self):
        self.frame_num += 1

    def transform_and_fetch_position(self, j):
        if j == "y_unit":
            return [0, 1, 0]
        elif j == "minus_y_unit":
            return [0, -1, 0]
        elif j == "zero":
            return [0, 0, 0]
        elif j == "y_min":
            return [
                0,
                min(
                    [y for (_, y, _) in self.positions[self.frame_num]]
                ),
                0,
            ]
        return self.positions[self.frame_num][
            self.joint_names.index(j)
        ]

    def transform_and_fetch_prev_position(self, j):
        return self.positions[self.frame_num - 1][
            self.joint_names.index(j)
        ]

    def f_move(self, j1, j2, j3, j4, range):
        j1_prev, j2_prev, j3_prev, j4_prev = [
            self.transform_and_fetch_prev_position(j) for j in [j1, j2, j3, j4]
        ]
        j1, j2, j3, j4 = [
            self.transform_and_fetch_position(j) for j in [j1, j2, j3, j4]
        ]
        return feat_utils.velocity_direction_above_threshold(
            j1, j1_prev, j2, j2_prev, j3, j3_prev, range,
        )

    def f_nmove(self, j1, j2, j3, j4, range):
        j1_prev, j2_prev, j3_prev, j4_prev = [
            self.transform_and_fetch_prev_position(j) for j in [j1, j2, j3, j4]
        ]
        j1, j2, j3, j4 = [
            self.transform_and_fetch_position(j) for j in [j1, j2, j3, j4]
        ]
        return feat_utils.velocity_direction_above_threshold_normal(
            j1, j1_prev, j2, j3, j4, j4_prev, range
        )

    def f_plane(self, j1, j2, j3, j4, threshold):
        j1, j2, j3, j4 = [
            self.transform_and_fetch_position(j) for j in [j1, j2, j3, j4]
        ]
        return feat_utils.distance_from_plane(j1, j2, j3, j4, threshold)

    # 
    def f_nplane(self, j1, j2, j3, j4, threshold):
        j1, j2, j3, j4 = [
            self.transform_and_fetch_position(j) for j in [j1, j2, j3, j4]
        ]
        return feat_utils.distance_from_plane_normal(j1, j2, j3, j4, threshold)

    # relative
    def f_angle(self, j1, j2, j3, j4, range):
        j1, j2, j3, j4 = [
            self.transform_and_fetch_position(j) for j in [j1, j2, j3, j4]
        ]
        return feat_utils.angle_within_range(j1, j2, j3, j4, range)

    # non-relative 
    def f_fast(self, j1, threshold):
        j1_prev = self.transform_and_fetch_prev_position(j1)
        j1 = self.transform_and_fetch_position(j1)
        return feat_utils.velocity_above_threshold(j1, j1_prev, threshold)
