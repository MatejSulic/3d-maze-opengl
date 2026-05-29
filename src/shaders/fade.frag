#version 460 core
out vec4 FragColor;
in vec2 TexCoord;

uniform float Progress;  

void main() {
    FragColor = vec4(0.0, 0.0, 0.0, Progress);
}