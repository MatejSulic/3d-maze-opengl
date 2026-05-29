#version 460 core

struct Material {
    vec3 color;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
    float shininess;
};

uniform vec3 ambientLight;
uniform Material material;
uniform vec3 cameraPosWorld;
uniform vec3 lightPosWorld;
uniform vec3 lightColor;
uniform float lightIntensity;
uniform vec3 lightDirection;
uniform float spotCutoff;
uniform float spotExponent;

in vec3 normalWorld;
in vec3 fragmentWorld;

out vec4 outColor;

void main() {

    vec3 primaryColor = material.color * ambientLight;

    vec3 lightDir = normalize(lightPosWorld - fragmentWorld);
    vec3 spotDir = normalize(lightDirection);

    float spotEffect = dot(spotDir, -lightDir);

    if (spotEffect > spotCutoff) {
        float distance = length(lightPosWorld - fragmentWorld);// vzdalenost od zdroje svetla
        float attenuation = clamp(1.0 / (distance * distance), 0.0, 1.0)*lightIntensity; // utlum podle vzdalenosti a intensity svetla
        vec3 finalColor = material.color * lightColor * attenuation;
        if(length(finalColor) < length(ambientLight*material.color)) {
		    finalColor = primaryColor;
	    }

        float spotlightFactor = pow((spotEffect - spotCutoff) / (1.0 - spotCutoff), spotExponent);
        vec3 endColor = mix(primaryColor, finalColor, spotlightFactor);
        outColor = vec4(endColor, 1.0);

    } else {
        outColor = vec4(primaryColor, 1.0);
    }
}
