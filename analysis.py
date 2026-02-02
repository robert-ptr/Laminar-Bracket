from convokit import Corpus, download
import kagglehub
import pandas as pd
import pickle
import numpy as np
import os

corpus = Corpus(filename=download("movie-corpus"))
utterances = corpus.get_utterances_dataframe()

with open("chat_data_for_csharp.txt", "w", encoding="utf-8") as f:
    for conv_id in corpus.get_conversation_ids()[:2000]:
        conversation = corpus.get_conversation(conv_id)
        f.write(f"--- CONVERSATION START: {conv_id} ---\n")
        for utt in conversation.iter_utterances():
            if utt.text:
                f.write(f"{utt.text}\n")
        f.write("--- CONVERSATION END ---\n\n")

splits = {'train': 'data/train-00000-of-00001.parquet', 'validation': 'data/validation-00000-of-00001.parquet'}
df_twitch = pd.read_parquet("hf://datasets/lparkourer10/twitch_chat/" + splits["train"])

with open("twitch_chat_raw.txt", "w", encoding="utf-8") as f:
    col = 'text' if 'text' in df_twitch.columns else df_twitch.columns[0]
    for msg in df_twitch[col].dropna().iloc[:20000]:
        f.write(f"{msg}\n")

path = kagglehub.dataset_download("sammahoney/esa-anomaly-dataset")
mission, channel_id = 1, 1
channel_file = os.path.join(path, f'ESA-Mission{mission}', f'ESA-Mission{mission}', 'channels', f'channel_{channel_id}', f'channel_{channel_id}')

with open(channel_file, 'rb') as f:
    esa_data = pickle.load(f)
df_esa = pd.DataFrame(esa_data)
esa_values = df_esa.iloc[:, 0].values.astype(np.float32)

with open("esa_telemetry.bin", "wb") as f:
    f.write(esa_values.tobytes())

for f_path in ["chat_data_for_csharp.txt", "twitch_chat_raw.txt", "esa_telemetry.bin", "minecraft_dataset.bin"]:
    print(f"{f_path}: {os.path.getsize(f_path) / 1024:.2f} KB")