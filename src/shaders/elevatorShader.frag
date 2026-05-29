#version 330 core

uniform float alpha;
in vec2 vTexCoord;
out vec4 FragColor;

void main()
{
    // Calculate door positions (0 = open, 1 = closed)
    float doorWidth = smoothstep(0.0, 1.0, alpha);
    
    // Check if current fragment is within door areas
    if (vTexCoord.x < doorWidth/2.0 || vTexCoord.x > 1.0 - doorWidth/2.0)
    {
        FragColor = vec4(0.0, 0.0, 0.0, 1.0); // Black color for doors
    }
    else
    {
        FragColor = vec4(0.0, 0.0, 0.0, 0.0); // Transparent for middle area
    }
}