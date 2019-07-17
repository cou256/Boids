struct TransformStruct{
	float3 translate;
	float4 rotation;
	float3 velocity;
	float3 center;
	uint centerCount;
	float3 separate;
    uint separateCount;
	float3 velocitySum;
    uint velocitySumCount;
};

RWStructuredBuffer<TransformStruct> _TransformBuff;
float3 _Bounds;
float _Speed;
float _SeparateWeight;
float _CohesionWeight;
float _AlignWeight;
float _FeedWeight;
float _BoundaryWeight;
float _SeparateNeighborDistance;
float _CohesionNeighborDistance;
float _AlignNeighborDistance;
float _FeedNeighborDistance;
float3 _FeedPosition;

const float3 ZERO = float3(0.0,0.0,0.0);

/*
 Reference
 https://stackoverflow.com/questions/12964279/whats-the-origin-of-this-glsl-rand-one-liner
*/
float rand(uint3 id : SV_DispatchThreadID, float salt) {
	return frac(sin(dot(id.xy, float2(12.9898, 78.233))) * 43758.5453 * salt);
}
void rotate(
	uint3 Gid : SV_GroupID,
	uint3 DTid : SV_DispatchThreadID,
	uint3 GTid : SV_GroupThreadID,
	uint GI : SV_GroupIndex
) {
	float3 v = _TransformBuff[DTid.x].velocity;
    _TransformBuff[DTid.x].rotation = look_at(v, float3(0.0, 1.0, 0.0));
}
float3 boundary(
	uint3 Gid : SV_GroupID,
	uint3 DTid : SV_DispatchThreadID,
	uint3 GTid : SV_GroupThreadID,
	uint GI : SV_GroupIndex
) {
	float3 t = _TransformBuff[DTid.x].translate;
	float3 v = _TransformBuff[DTid.x].velocity;
	float3 b = _Bounds * 0.5;
	float3 desired = float3(0.0,0.0,0.0);
	if(t.x < -b.x){
		desired.x +=  1.0;
	}
	if(t.x >  b.x){
		desired.x += -1.0;
	}
	if(t.y < -b.y){
		desired.y +=  1.0;
	}
	if(t.y >  b.y){
		desired.y += -1.0;
	}
	if(t.z < -b.z){
		desired.z +=  1.0;
	}
	if(t.z >  b.z){
		desired.z += -1.0;
	}
	return desired;
}
float3 separate(
	uint3 Gid : SV_GroupID,
	uint3 DTid : SV_DispatchThreadID,
	uint3 GTid : SV_GroupThreadID,
	uint GI : SV_GroupIndex
) {
	if(_TransformBuff[DTid.x].separateCount > 0)
	{
		return normalize(_TransformBuff[DTid.x].separate / _TransformBuff[DTid.x].separateCount);
	}
	return ZERO;
}
float3 cohesion(
	uint3 Gid : SV_GroupID,
	uint3 DTid : SV_DispatchThreadID,
	uint3 GTid : SV_GroupThreadID,
	uint GI : SV_GroupIndex
) {
	if(_TransformBuff[DTid.x].centerCount > 0)
	{
		float3 center = _TransformBuff[DTid.x].center / _TransformBuff[DTid.x].centerCount;
		return normalize(center - _TransformBuff[DTid.x].translate);
	}
	return ZERO;
}
float3 align(
	uint3 Gid : SV_GroupID,
	uint3 DTid : SV_DispatchThreadID,
	uint3 GTid : SV_GroupThreadID,
	uint GI : SV_GroupIndex
) {
	if(_TransformBuff[DTid.x].velocitySumCount > 0 && length(_TransformBuff[DTid.x].velocitySum) > 0)
	{
		return normalize(_TransformBuff[DTid.x].velocitySum / _TransformBuff[DTid.x].velocitySumCount);
	}
	return ZERO;
}
float3 feed(
	uint3 Gid : SV_GroupID,
	uint3 DTid : SV_DispatchThreadID,
	uint3 GTid : SV_GroupThreadID,
	uint GI : SV_GroupIndex
) {
	float3 diff = _FeedPosition - _TransformBuff[DTid.x].translate;
	float dist = length(diff);
	if(dist < _FeedNeighborDistance)
	{
		return normalize(diff);
	}
	return ZERO;
}
void motion(
	uint3 Gid : SV_GroupID,
	uint3 DTid : SV_DispatchThreadID,
	uint3 GTid : SV_GroupThreadID,
	uint GI : SV_GroupIndex
) {
	float3 force
		= (separate(Gid, DTid, GTid, GI) * _SeparateWeight)
		+ (cohesion(Gid, DTid, GTid, GI) * _CohesionWeight)
		+ (align(Gid, DTid, GTid, GI)	 * _AlignWeight)
		+ (feed(Gid, DTid, GTid, GI)	 * _FeedWeight)
		+ (boundary(Gid, DTid, GTid, GI) * _BoundaryWeight);
	_TransformBuff[DTid.x].velocity += force * unity_DeltaTime.x;
	float mag = length(_TransformBuff[DTid.x].velocity);
	if(mag > _Speed){
		_TransformBuff[DTid.x].velocity *= (_Speed / mag);
	}
	if(mag == 0){
		_TransformBuff[DTid.x].velocity = float3(
			rand(DTid, rand(DTid, dot(_Time.x,_Time.y))),
			rand(DTid, rand(DTid, dot(_Time.y,_Time.z))),
			rand(DTid, rand(DTid, dot(_Time.z,_Time.w)))) * 10.0;
	}
	_TransformBuff[DTid.x].translate += _TransformBuff[DTid.x].velocity * unity_DeltaTime.x;
}
void finalize(
	uint3 Gid : SV_GroupID,
	uint3 DTid : SV_DispatchThreadID,
	uint3 GTid : SV_GroupThreadID,
	uint GI : SV_GroupIndex
) {
	_TransformBuff[DTid.x].separate = ZERO;
	_TransformBuff[DTid.x].separateCount = 0.0;
	_TransformBuff[DTid.x].center = ZERO;
	_TransformBuff[DTid.x].centerCount = 0.0;
	_TransformBuff[DTid.x].velocitySum = ZERO;
	_TransformBuff[DTid.x].velocitySumCount = 0.0;
}
void initialize(
	uint3 Gid : SV_GroupID,
	uint3 DTid : SV_DispatchThreadID,
	uint3 GTid : SV_GroupThreadID,
	uint GI : SV_GroupIndex
) {
	_TransformBuff[DTid.x].translate = (float3(
		rand(DTid, rand(DTid, dot(_Time.x,_Time.y))),
		rand(DTid, rand(DTid, dot(_Time.y,_Time.z))),
		rand(DTid, rand(DTid, dot(_Time.z,_Time.w)))) - 0.5) * _Bounds;
	_TransformBuff[DTid.x].velocity = (float3)0.0;
}
void forceCompute(
	uint3 Gid : SV_GroupID,
	uint3 DTid : SV_DispatchThreadID,
	uint3 GTid : SV_GroupThreadID,
	uint GI : SV_GroupIndex
) {
    float3 diff = _TransformBuff[DTid.x].translate - _TransformBuff[DTid.y].translate;
	float dist = length(diff);
	if (dist > 0.0 && dist < _SeparateNeighborDistance)
	{
		_TransformBuff[DTid.x].separate += (normalize(diff) / dist);
		_TransformBuff[DTid.x].separateCount++;
	}
	if (dist > 0.0 && dist < _CohesionNeighborDistance)
	{
		_TransformBuff[DTid.x].center += _TransformBuff[DTid.y].translate;
		_TransformBuff[DTid.x].centerCount++;
	}
	if (dist > 0.0 && dist < _AlignNeighborDistance)
	{
		_TransformBuff[DTid.x].velocitySum += _TransformBuff[DTid.y].velocity;
		_TransformBuff[DTid.x].velocitySumCount++;
	}
}
void boidsCompute(
	uint3 Gid : SV_GroupID,
	uint3 DTid : SV_DispatchThreadID,
	uint3 GTid : SV_GroupThreadID,
	uint GI : SV_GroupIndex
) {
    motion(Gid, DTid, GTid, GI);
	rotate(Gid, DTid, GTid, GI);
	finalize(Gid, DTid, GTid, GI);
}