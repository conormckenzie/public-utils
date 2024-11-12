#!/bin/bash

sudo apt update -y
sudo apt install python-is-python3 -y
sudo apt install python3-pip -y
sudo apt install pip -y

# Enable if you wnat to auto-run the combine code script.
./run_combine_code.sh