shader_type spatial;

uniform float height_scale = 5.0;
uniform sampler2D noise_tex;

vec3 get_offset(vec2 uv) {
    return height_scale * vec3(0, texture(noise_tex, uv).r - 0.5, 0);
}

void vertex() {
    vec3 offset = get_offset(UV);
    VERTEX += offset;

    const vec2 epsilon = vec2(0.01, 0);
    vec3 offset_right = get_offset(UV + epsilon.xy);
    vec3 offset_up = get_offset(UV + epsilon.yx);

    vec3 tangent = normalize(offset_right - offset + epsilon.xyy);
    vec3 binormal = normalize(offset_up - offset + epsilon.yyx);
    NORMAL = normalize(cross(binormal, tangent));
}
void fragment() {
	ALBEDO = 0.5 + 0.5 * (mat3(INV_VIEW_MATRIX) * NORMAL);
}
