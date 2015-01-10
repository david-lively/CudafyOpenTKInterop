#version 150 core
/// see http://www.lighthouse3d.com/cg-topics/code-samples/opengl-3-3-glsl-1-5-sample/


uniform sampler2D Texture;

in vec4 worldPosition;
in vec2 texCoord;

out vec4 fragmentColor;

void main() {
	vec4 texel = texture2D(Texture,texCoord);
	fragmentColor = texel;
}