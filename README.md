# MNNSeg
MNN Segmentation inference code

## Dependencies
- OpenCV
> In ubuntu, it is easy to config the dependency of OpenCV by run the following code 
> ```
> sudo apt-get install libopencv-dev
> ```

## Usage 
```
mkdir build && cd build
cmake ..
make
./segmnn <model_path> <image_path>
```

