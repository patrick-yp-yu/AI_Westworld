from flask import Flask
from transformers import AutoModelForSeq2SeqLM, AutoTokenizer
import torch

import json

import logging
import tensorflow as tf
import tensorflow_hub as hub
from sklearn.metrics.pairwise import cosine_similarity
import time

module_path = "universal-sentence-encoder_4"
sim_model = hub.load(module_path)


def process(text):
    # require completion

    create_list = []
    # modify_list = []
    delete_list = {}
    sentences = [text, "sphere", "cube", "red", "blue", "green", "white", "small", "medium", "large"]
    embeddings = sim_model(sentences)
    similarity = cosine_similarity(embeddings, embeddings)[0]

    color_arr = similarity[3:7]
    color_max = -1
    color_idx = 0
    for i in range(4):
        if color_arr[i] > color_max:
            color_max = color_arr[i]
            color_idx = i

    size_arr = similarity[7:]
    size_max = -1
    size_idx = 0
    for i in range(3):
        if size_arr[i] > size_max:
            size_max = size_arr[i]
            size_idx = i

    type_arr = similarity[1:3]
    type_max = -1
    type_idx = 0
    for i in range(2):
        if type_arr[i] > type_max:
            type_max = type_arr[i]
            type_idx = i

    create_list.append({"Name": "object1", "Type": sentences[1 + type_idx], "Color": sentences[3 + color_idx],
                        "Size": sentences[7 + size_idx], "Location": "default"})

    rt = json.dumps({"Text": text, "Object": {"Create": create_list, "Delete": delete_list}})
    return rt


app = Flask(__name__)
tokenizer = AutoTokenizer.from_pretrained("facebook/blenderbot-400M-distill")
input_model = AutoModelForSeq2SeqLM.from_pretrained("facebook/blenderbot-400M-distill")


@app.route('/<name>')
def idx(name):
    inputs = tokenizer(name, return_tensors="pt")
    tf.random.set_seed(0)  # for testing purposes
    results = input_model.generate(
        **inputs,
        do_sample=True,
        top_p=0.92,
        top_k=50,
    )
    # print(tokenizer.decode(results[0]))
    # tokenizer.batch_decode(reply_ids)
    return process(tokenizer.decode(results[0], skip_special_tokens=True))


if __name__ == '__main__':
    app.run()
