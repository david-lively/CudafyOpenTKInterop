#version 150 core
/// http://www.lighthouse3d.com/cg-topics/code-samples/opengl-3-3-glsl-1-5-sample/
uniform mat4 WorldMatrix;
uniform mat4 ViewMatrix;
uniform mat4 ProjectionMatrix;

in vec3 Position;
in vec2 TextureCoordinates;

out vec4 worldPosition;
out vec2 texCoord;

void main()
{
	//texCoord = TextureCoordinates;
	/// create texture coordinates from the vertex positions.
	/// see GeometryProvider.Quad() for vertex positions
	texCoord.x = Position.x + 0.5;
	texCoord.y = 0.5 - Position.y;
	worldPosition = ProjectionMatrix * ViewMatrix * WorldMatrix * vec4(Position,1);
	
	gl_Position = worldPosition;
}
