# Bailando
Code for TPAMI 2023 paper "Bailando++: 3D dance GPT with Choreographic Memory"


[[Paper]](https://ieeexplore.ieee.org/abstract/document/10264209) | [[Video Demo]](https://youtu.be/jht6NpwqLM4)

<p float="center">
	<img src="https://github.com/lisiyao21/Bailando/blob/main/gifs/improvement.gif" width="850" /> 
	</p>

> Our proposed music-to-dance framework, Bailando++, addresses the challenges of driving 3D characters to dance in a way that follows the constraints of choreography norms and maintains temporal coherency with different music genres. Bailando++ consists of two components: a choreographic memory that learns to summarize meaningful dancing units from 3D pose sequences, and an actor-critic Generative Pre-trained Transformer (GPT) that composes these units into a fluent dance coherent to the music. In particular, to synchronize the diverse motion tempos and music beats, we introduce an actor-critic-based reinforcement learning scheme to the GPT with a novel beat-align reward function. Additionally, we consider learning human dance poses in the rotation domain to avoid body distortions incompatible with human morphology, and introduce a musical contextual encoding to allow the motion GPT to grasp longer-term patterns of music. Our experiments on the standard benchmark show that Bailando++ achieves state-of-the-art performance both qualitatively and quantitatively, with the added benefit of the unsupervised discovery of human-interpretable dancing-style poses in the choreographic memory.

# Code

## Environment
    PyTorch == 1.6.0

## Data preparation

The data format keep the same as the original Bailando. Besides, we need additional rotmat motion data to train the new VQVAE decoder. Please run

    ./prepare_aistpp_data.sh

to generate the rotmat data or directly download from [here] to ../data.

## Training

Based on original Bailando, Bailando++ introduce a new decoder branch to decode the quantized codes to rotmat sequence, which is much easier to drive avatar than 3D positions. Moreover, it includes a music Transformer to encode the context of music. Upon pretrained Bailando, the training processes of Bailando++ include 3 steps 

<!-- If you are using the slurm workload manager, run the code as

If not, run -->

### Step 1: Train rotmat VQVAE decoder

    sh srun_mix.sh configs/sep_vqvae_root_mix.yaml.yaml train [your node name] 1

### Step 2: Train motion gpt (with music Transformer)

    sh srun_gpt_all.sh configs/cc_motion_gpt_music_trans.yaml train [your node name] 1

### Step 3: Actor Critic

    sh srun_actor_critic.sh configs/actor_critic_music_trans_400.yaml train [your node name] 1


## Evaluation

To test with our pretrained models, please download and release the weights from [here](https://drive.google.com/file/d/1jepf20XuplVSGEfwaPaqwniyl4styvqa/view?usp=sharing) (Google Drive) under ./experiments folder.

### 1. Generate dancing results

Run
    
    sh srun_actor_critic.sh configs/actor_critic_music_trans_400_mix_rotmat.yaml eval [your node name] 1

The rotmat sequences are finally transfered to SMPL sequences and are stored in ''experiments/[exp name]/eval/motion/ep[epoch num]/''. The SMPL sequences can be then rendered by Blender using [SMPLX addon](https://github.com/Meshcapade/SMPL_blender_addon) and [Auto-Rig Pro](https://blendermarket.com/products/auto-rig-pro). 3D joint positions are stored in ''experiments/[exp name]/eval/pkl/ep[epoch num]/''. 

Please note that in test config ''actor_critic_music_trans_400_mix_rotmat.yaml'' we use the VQVAE with new leared rotmat decoder (SepVQVAERmix). It is also feasible to regenerate the original Bailando's results into SMPL format using the new learned decoder. 


### 2. Dance quality evaluations

The metrics are the same as those in original Bailando.

### compute the evaluation metrics:

    python utils/metrics_new.py

<!-- It will show exactly the same values reported in the paper. To fasten the computation, comment Line 184 of utils/metrics_new.py after computed the ground-truth feature once. To test another folder, change Line 182 to your destination, or kindly modify this code to a "non hard version" :)
 -->


### Citation

    @ARTICLE{siyao2023bailandopp,
        author={Siyao, Li and Yu, Weijiang and Gu, Tianpei and Lin, Chunze and Wang, Quan and Qian, Chen and Loy, Chen Change and Liu, Ziwei},
        journal={IEEE Transactions on Pattern Analysis and Machine Intelligence}, 
        title={Bailando++: 3D Dance GPT With Choreographic Memory}, 
        year={2023},
        volume={45},
        number={12},
        pages={14192-14207},
        doi={10.1109/TPAMI.2023.3319435}}

### License

Our code is released under MIT License.

