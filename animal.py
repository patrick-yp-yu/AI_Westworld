import json
import requests
import math
from flask import Flask, request
import tensorflow_hub as hub
from sklearn.metrics.pairwise import cosine_similarity
from transformers import AutoModelForSeq2SeqLM, AutoTokenizer

API_TOKEN = "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"

API_URL = "https://api-inference.huggingface.co/models/vblagoje/bert-english-uncased-finetuned-pos"
headers = {"Authorization": f"Bearer {API_TOKEN}"}

module_url = "https://tfhub.dev/google/universal-sentence-encoder/4"
sim_model = hub.load(module_url)
# Commented out code for testing with local version of universal sentence encoder
# module_path = "universal-sentence-encoder_4"
# sim_model = hub.load(module_path)

tokenizer = AutoTokenizer.from_pretrained("facebook/blenderbot-400M-distill")
input_model = AutoModelForSeq2SeqLM.from_pretrained("facebook/blenderbot-400M-distill")

OBJ_ID = 1
OBJ_LIST = []
# Setting default object type and mode values for testing
# These values are overwritten by unity requests
TYPE = ["Cow", "Chicken", "Sheep", "Duck", "Pig"]
MODE = '0'


def query(payload):
    response = requests.post(API_URL, headers=headers, json=payload)
    return response.json()


def create_obj(obj, adj):
    global OBJ_ID
    global OBJ_LIST
    go = {"Name": obj + "_" + str(OBJ_ID), "Type": obj, "Location": adj["Location"], "Color": adj["Color"],
          "Size": adj["Size"]}
    OBJ_LIST.append(go)
    OBJ_ID += 1
    return go


def delete_obj(obj, adj):
    global OBJ_LIST
    target = []
    for i in OBJ_LIST:
        if i["Type"] == obj:
            if i["Color"] == adj["Color"] and i["Size"] == adj["Size"]:
                target.append(i["Name"])
                OBJ_LIST.remove(i)
                break
    return target


def find_adj_numeric(part):
    # defining possible adjective by numerical similarity
    global TYPE
    colors = ["black", "blue", "cyan", "gray", "green", "magenta", "red", "white", "yellow"]
    sizes = ["small tiny mini", "medium standard normal", "large huge big"]
    locations = ["far", "close", "front", "back", "right", "left", "high", "low"]
    adj = {"Color": "default", "Size": 1, "Location": [0, 0, 0]}

    c = [part] + colors
    embeddings = sim_model(c)
    similarity = cosine_similarity(embeddings, embeddings)[0][1:]
    mv = max(similarity)
    if mv > 0.1:
        mi = list(similarity).index(mv)
        adj["Color"] = colors[mi]

    s = [part] + sizes
    embeddings = sim_model(s)
    similarity = cosine_similarity(embeddings, embeddings)[0][1:]
    size_arr = similarity
    size_total_sim = sum(map(abs, size_arr))
    size_calc = 60.0
    small_calc = ((size_arr[0] * (1 - size_arr[1] / size_total_sim)) * 60)
    large_calc = ((size_arr[2] * (1 - size_arr[1] / size_total_sim)) * 60)
    if small_calc < 0:
        size_calc += abs(small_calc) ** 1.6
    else:
        size_calc -= small_calc ** 1.6
    if large_calc < 0:
        size_calc -= abs(large_calc) ** 1.6
    else:
        size_calc += large_calc ** 1.6
    size_calc /= 30
    if size_calc < 0.05:
        size_calc = 0.05
    # mv = max(similarity)
    # if mv > 0.1:
    adj["Size"] = size_calc

    l = [part] + locations
    embeddings = sim_model(l)
    similarity = cosine_similarity(embeddings, embeddings)[0][1:]
    location_arr = similarity
    i = 0
    while i < len(location_arr):
        smaller = location_arr[i]
        larger = location_arr[i + 1]
        if smaller < 0:
            location_arr[i + 1] -= smaller
            location_arr[i] -= smaller
        if larger < 0:
            location_arr[i] -= larger
            location_arr[i + 1] -= larger
        i += 2
    distance = (60.0 + (location_arr[0] * 60) ** 1.6 - (location_arr[1] * 60) ** 1.6) / 6.0
    height = 5 + (3.0 + (distance * location_arr[6] * 3) - (distance * location_arr[7] * 3)) / 3.0
    x = distance * (0.0 + (location_arr[2] * 40) ** 1.6 - (location_arr[3] * 40) ** 1.6) / 6.0
    z = distance * (0.0 + (location_arr[4] * 40) ** 1.6 - (location_arr[5] * 40) ** 1.6) / 6.0
    direction = math.atan2(z, x) * (180 / math.pi)
    adj["Location"] = [height, distance, direction]

    return adj


def find_adj(part):
    # defining adjectives by picking the most similar category
    global TYPE
    colors = ["black", "blue", "cyan", "gray", "green", "magenta", "red", "white", "yellow"]
    sizes = ["small tiny mini", "medium standard normal", "large huge big"]
    locations = ["far", "close", "front", "back", "right", "left", "high", "low"]
    adj = {"Color": "default", "Size": 1, "Location": [0, 0, 0]}

    c = [part] + colors
    embeddings = sim_model(c)
    similarity = cosine_similarity(embeddings, embeddings)[0][1:]
    mv = max(similarity)
    if mv > 0.1:
        mi = list(similarity).index(mv)
        adj["Color"] = colors[mi]

    s = [part] + sizes
    embeddings = sim_model(s)
    similarity = cosine_similarity(embeddings, embeddings)[0][1:]
    mv = max(similarity)
    if mv > 0.1:
        mi = list(similarity).index(mv)
        adj["Size"] = [1, 2, 3][mi]

    l = [part] + locations
    embeddings = sim_model(l)
    similarity = cosine_similarity(embeddings, embeddings)[0][1:]
    # distance
    if similarity[0] > similarity[1]:
        adj["Location"][1] = 5
    else:
        adj["Location"][1] = 1
    # height
    if similarity[6] > similarity[7]:
        adj["Location"][0] = 10
    else:
        adj["Location"][0] = 5
    # direction
    mv = max(similarity[2:6])
    if similarity[2] == mv:
        adj["Location"][2] = 0
    elif similarity[3] == mv:
        adj["Location"][2] = 180
    elif similarity[4] == mv:
        adj["Location"][2] = 90
    elif similarity[5] == mv:
        adj["Location"][2] = 270

    return adj


def process(text):
    global OBJ_ID
    global OBJ_LIST
    global TYPE

    OP = ["create", "delete"]
    create_list = []
    delete_list = []
    tags = query({"inputs": text})
    conj = []
    # divide text by conjuction and punctuations
    for i in range(len(tags)):
        if tags[i]['entity_group'] == "CCONJ" or tags[i]["word"] in ".?!:;":
            conj.append(i)
    parts = []
    for i in reversed(conj):
        tags, pt = tags[:i], tags[i:]
        parts.insert(0, pt)
    parts.insert(0, tags)
    
    pre = None # pre-recorded operation
    for p in parts:
        # for each part, find verb, noun and adjectives
        op = None
        ob = None
        thres = 0.1 # accuracy threshold
        adj = []
        for word in p:
            # check operation
            if word["entity_group"] == "VERB":
                l = [word["word"], "create", "delete"]
                embeddings = sim_model(l)
                similarity = cosine_similarity(embeddings, embeddings)[0][1:]
                mv = max(similarity)
                if mv > thres:
                    mi = list(similarity).index(mv)
                    op = OP[mi]
            # check whether object matches the list elements
            if word["entity_group"] == "NOUN":
                l = [" ".join([i["word"] for i in p])] + TYPE
                embeddings = sim_model(l)
                similarity = cosine_similarity(embeddings, embeddings)[0][1:]
                mv = max(similarity)
                if mv > thres:
                    mi = list(similarity).index(mv)
                    ob = TYPE[mi]
                    thres = mv

        # toggle for numerical
        adj = find_adj_numeric(" ".join([i["word"] for i in p])) if MODE == '2' else find_adj(" ".join([i["word"] for i in p]))
        if ob and op == "create":
            create_list.append(create_obj(ob, adj))
            pre = op
        elif ob and op == "delete":
            result = delete_obj(ob, adj)
            if result:
                for i in result:
                    delete_list.append(i)
            pre = op
        elif ob and pre == "create":
            create_list.append(create_obj(ob, adj))
        elif ob and pre == "delete":
            result = delete_obj(ob, adj)
            if result:
                for i in result:
                    delete_list.append(i)

    rt = json.dumps({"Text": text, "Object": {"Create": create_list, "Delete": delete_list}})
    return rt


app = Flask(__name__)


@app.route('/<name>')
def idx(name):
    global MODE
    MODE = name[0]
    # mode 0: process raw input; mode 1: process sbot response
    if MODE == "0":
        return process(name[1:])
    else:
        # have = set()
        # for i in OBJ_LIST:
        #     have.add(i[""])
        inputs = tokenizer(name[1:], return_tensors="pt")
        results = input_model.generate(
            **inputs,
            do_sample=True,
            top_p=0.92,
            top_k=50,
        )
        response = tokenizer.decode(results[0], skip_special_tokens=True)
        return process(response)


@app.route('/prefab')
# set TYPE list
def index():
    global TYPE
    p = request.args.get('list')
    TYPE = p.split(',')
    print(p)
    return '0'


if __name__ == '__main__':
    app.run()
