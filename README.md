# Bailando
Code for CVPR 2022 paper "Bailando: 3D dance generation via Actor-Critic GPT with Choreographic Memory"

[[Paper]](https://arxiv.org/abs/2203.13055) | [Project Page] |  [[Video Demo]](https://www.youtube.com/watch?v=YbXOcuMTzD8)

<img width=18% src="https://github.com/lisiyao21/Bailando/blob/main/gifs/dance_gif1.gif"/> <img width=44% src="https://github.com/lisiyao21/Bailando/blob/main/gifs/dance_gif2.gif"/> <img width=33% src="https://github.com/lisiyao21/Bailando/blob/main/gifs/dance_gif3.gif"/>

> Driving 3D characters to dance following a piece of music is highly challenging due to the **spatial** constraints applied to poses by choreography norms. In addition, the generated dance sequence also needs to maintain **temporal** coherency with different music genres. To tackle these challenges, we propose a novel music-to-dance framework, **Bailando**, with two powerful components: **1)** a choreographic memory that learns to summarize meaningful dancing units from 3D pose sequence to a quantized codebook, **2)** an actor-critic Generative Pre-trained Transformer (GPT) that composes these units to a fluent dance coherent to the music. With the learned choreographic memory, dance generation is
realized on the quantized units that meet high choreography standards, such that the generated dancing sequences are confined within the spatial constraints. To achieve synchronized alignment between diverse motion tempos and music beats, we introduce an actor-critic-based reinforcement learning scheme to the GPT  with a newly-designed beat-align reward function. Extensive experiments on the standard benchmark demonstrate that our proposed framework achieves state-of-the-art performance both qualitatively and quantitatively. Notably, the learned choreographic memory is shown to discover human-interpretable dancing-style poses in an unsupervised manner.

# Code

TODO: user guide and implementation instructions 

## Environment
(TODO)

## Data preparation

In our experiments, we use AIST++ for both training and evaluation. Please visit [here](https://google.github.io/aistplusplus_dataset/download.html) to download the AIST++ annotations and unzip them as './aist_plusplus_final/' folder, visit [here](https://aistdancedb.ongaaccel.jp/database_download/) to download the original all music pieces (mp3) into './aist_plusplus_final/all_musics', and finally run

to produce the features for training and test.


## Training

If you are using the slurm workload manager, run the code as



If not, run



### Step 1: Train pose VQ-VAE (without global velocity)




### Step 2: Train glabal velocity branch of pose VQ-VAE

### Step 3: Train motion GPT
### Step 4: Actor-Critic finetuning on target music 

## Evaluation

### 1. Generate dancing results

### 2. Dance quality evaluations

## Choreographic for music in the wild



### Citation

    @inproceedings{siyao2022bailando,
	    title={Bailando: 3D dance generation via Actor-Critic GPT with Choreographic Memory,
	    author={Siyao, Li and Yu, Weijiang and Gu, Tianpei and Lin, Chunze and Wang, Quan and Qian, Chen and Loy, Chen Change and Liu, Ziwei },
	    booktitle={CVPR},
	    year={2022}
    }

### License

Our code is released under MIT License.

