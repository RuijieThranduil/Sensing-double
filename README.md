# Sensing-double

This project is a collaborative graduation design by Ruijie Thranduil, Zongqi He, and Yuan Zhang. It aims to explore basic multimodal interaction using camera-based color recognition and sound-to-image generation. The implementation relies on relevant open-source tools and AI models. The overall design and implementation are still being continuously improved, and feedback from teachers and fellow students is welcome.

## Project Overview

- **Color Recognition**: Uses a webcam to detect specified colors in real-time, serving as input or triggers for subsequent processes.
- **Sound-to-Image Generation**: Adopts the training approach from the [Soundscape-to-Image](https://github.com/GISense/Soundscape-to-Image) project to convert sound signals into images. The actual image generation uses the GPTImage1 model API.
- **The GPTImage1 API supports inpainting (partial redraw) based on a mask + prompt, enabling precise transformation and region-specific editing in the generated visuals. https://platform.openai.com/docs/guides/image-generation?image-generation-model=gpt-image-1
## Usage Instructions

### 1. Clone the Project

```bash
git clone https://github.com/RuijieThranduil/Sensing-double.git
```

### 2. Sound-to-Image Module Configuration

1. **Clone the Sound-to-Image Dependency Project**  
   Please first clone the [Soundscape-to-Image](https://github.com/GISense/Soundscape-to-Image) repository and follow its documentation to train the model or prepare the necessary weights.

   ```bash
   git clone https://github.com/GISense/Soundscape-to-Image.git
   ```

2. **Integrate into Unity Project**  
   Add the relevant code and model weights from Soundscape-to-Image into your Unity project's directories (such as `Assets`). This can serve as the template module for sound-to-image generation.

3. **Configure GPTImage1 Image Generation API**  
   Configure the GPTImage1 model according to the API documentation for converting sound signals into images:
   https://platform.openai.com/docs/guides/image-generation?image-generation-model=gpt-image-1

### 3. Customizing Colors and Prompts

In the **GameManagement** script, you can customize the colors recognized by the camera and their corresponding prompts. Flexible configuration is supported for future extensions.

### 4. Follow UI Instructions

After launching the project, simply follow the step-by-step instructions on the UI to complete configuration, recognition, and image generation.

## Main Dependencies

- Unity 2021 or above
- [Soundscape-to-Image](https://github.com/GISense/Soundscape-to-Image)
- GPTImage1 Image Generation API

## Directory Structure

```
Sensing-double/
├── Assets/
├── GameManagement/         # Color recognition and prompt configuration
├── Soundscape-to-Image/    # Needs to be cloned separately
├── README.md
└── ...
```

## Acknowledgements

Special thanks to the Technical University of Munich (TUM), Architecture Information Chair, Nick Foester, Ivan, and Professor Frank Petzold for their support and guidance on this project.

Thanks also to the open-source community and related projects for their technical support, as well as to teachers for their guidance. Suggestions and feedback are welcome.

## License

Please see the LICENSE file in this repository.
