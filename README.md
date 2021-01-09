# MNNSeg
MNN Segmentation inference code

## Dependencies
- OpenCV
> In ubuntu, it is easy to config the dependency of OpenCV by run the following code 
> ```
> sudo apt-get install libopencv-dev
> ```

## Usage 
### Step 1. Build the libMNN.so from the [MNN](https://github.com/alibaba/MNN)
> Btw, in my implementation, I only build the cpu dynamic library for test the code. So you may adjust the code for fullfill your need.

### Step 2. Place the built `.so` file ubder the lib folder

### Step 3. Build the executable file

```
mkdir build && cd build
cmake ..
make
./segmnn <model_path> <image_path>
```

