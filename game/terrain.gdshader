shader_type spatial;

uniform float height_scale = 5.0; 
uniform sampler2D noise_tex;

float get_height(vec2 uv, float time) {
    return height_scale * (texture(noise_tex, uv).r - 0.5);
}

void vertex() {
    float height = get_height(UV, TIME);
    VERTEX.y += height;

    const vec2 epsilon = vec2(0.01, 0);
    float height_right = get_height(UV + epsilon.xy, TIME);
    float height_up = get_height(UV + epsilon.yx, TIME);

    vec3 tangent = normalize(vec3(epsilon.x, height_right - height, 0));
    vec3 binormal = normalize(vec3(0, height_up - height, epsilon.x));
    NORMAL = normalize(cross(binormal, tangent));
}
void fragment() {
	ALBEDO = 0.5 + 0.5 * (INV_VIEW_MATRIX * vec4(NORMAL, 0)).rgb;
}