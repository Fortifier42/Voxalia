#version 430 core

layout (location = 0) in vec3 position;
layout (location = 1) in vec3 normal;
layout (location = 2) in vec2 texcoords;
layout (location = 3) in vec3 tangent;
layout (location = 4) in vec4 color;
layout (location = 5) in vec4 Weights;
layout (location = 6) in vec4 BoneID;
layout (location = 7) in vec4 Weights2;
layout (location = 8) in vec4 BoneID2;

const int MAX_BONES = 200;

layout (location = 1) uniform mat4 projection = mat4(1.0);
layout (location = 2) uniform mat4 model_matrix = mat4(1.0);
layout (location = 3) uniform vec4 v_color = vec4(1.0);
// ...
layout (location = 40) uniform mat4 simplebone_matrix = mat4(1.0);
layout (location = 41) uniform mat4 boneTrans[MAX_BONES];

out struct vox_out
{
	vec4 position;
	vec2 texcoord;
	vec4 color;
	mat3 tbn;
	vec2 scrpos;
	float z;
} f;

void main()
{
	vec4 pos1;
	vec4 nor1;
	float rem = 1.0 - (Weights[0] + Weights[1] + Weights[2] + Weights[3] + Weights2[0] + Weights2[1] + Weights2[2] + Weights2[3]);
	mat4 BT = mat4(1.0);
	if (rem < 0.99)
	{
		BT = boneTrans[int(BoneID[0])] * Weights[0];
		BT += boneTrans[int(BoneID[1])] * Weights[1];
		BT += boneTrans[int(BoneID[2])] * Weights[2];
		BT += boneTrans[int(BoneID[3])] * Weights[3];
		BT += boneTrans[int(BoneID2[0])] * Weights2[0];
		BT += boneTrans[int(BoneID2[1])] * Weights2[1];
		BT += boneTrans[int(BoneID2[2])] * Weights2[2];
		BT += boneTrans[int(BoneID2[3])] * Weights2[3];
		BT += mat4(1.0) * rem;
		pos1 = vec4(position, 1.0) * BT;
		nor1 = vec4(normal, 1.0) * BT;
	}
	else
	{
		pos1 = vec4(position, 1.0);
		nor1 = vec4(normal, 1.0);
	}
	pos1 *= simplebone_matrix;
	nor1 *= simplebone_matrix;
	f.color = color;
    if (f.color == vec4(0.0, 0.0, 0.0, 1.0))
    {
        f.color = vec4(1.0);
    }
    f.color = f.color * v_color;
	f.texcoord = texcoords;
	vec4 tpos = model_matrix * vec4(pos1.xyz, 1.0);
	f.position = tpos / tpos.w;
	vec4 npos = projection * tpos;
	f.scrpos = npos.xy / npos.w * 0.5 + vec2(0.5);
	f.z = npos.z;
	gl_Position = npos;
	mat4 mv_mat_simple = model_matrix;
	mv_mat_simple[3][0] = 0.0;
	mv_mat_simple[3][1] = 0.0;
	mv_mat_simple[3][2] = 0.0;
	vec3 tf_normal = (mv_mat_simple * vec4(nor1.xyz, 0.0)).xyz; // TODO: Should BT be here?
	vec3 tf_tangent = (mv_mat_simple * vec4(tangent, 0.0)).xyz; // TODO: Should BT be here?
	vec3 tf_bitangent = (mv_mat_simple * vec4(cross(tangent, nor1.xyz), 0.0)).xyz; // TODO: Should BT be here?
	f.tbn = transpose(mat3(tf_tangent, tf_bitangent, tf_normal)); // TODO: Neccessity of transpose()?
}
