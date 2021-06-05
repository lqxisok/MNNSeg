# Windows Build Instruction

## MNN Build
> Time: 2021 1.18

### Environment Setup Steps：
1. download the MinGW from the following [url](https://sourceforge.net/projects/mingw-w64/files/mingw-w64/)
2. install the MinGW
3. install [ninja](https://github.com/ninja-build/ninja)
4. install [cmake](https://cmake.org/)

> Tips
> #### For MinGW
> 1. select the following options when install the MinGW
>   1. Architecture : x86_64
>   2. Threads : posix (important)
>   3. Exception : sjlj
> 2. add the `/path/to/MinGW/bin ` into your computer environment variable
> 
> #### For ninja
> 1. add the `/path/to/ninja/` into your computer environment variable (require to find the `ninja.exe`)
> 

### Build Steps:
1. clone the `MNN` into a path
2. open a powershell and execuate `cd /path/to/MNN`
3. run the command `Powershell.exe -executionpolicy remotesigned -File .\schema\generate.ps1`
4. run the command `mkdir build` and run `cd build`
5. run the command `cmake -G Ninja -DCMAKE_SYSTEM_PROCESSOR=$PROCESSOR_ARCHITECTURE -DCMAKE_SYSTEM_NAME=Windows -DCMAKE_BUILD_TYPE=Release -std=c++11 ..` (`-std=c++11` is important and I don't know is the `$PROCESSOR_ARCHITECTURE` is used or not. it just listed on the yuque doc.)
6. run `ninja` to build the lib and exe files