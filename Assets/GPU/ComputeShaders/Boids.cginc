struct TransformStruct{
	float3 translate;
	float3 rotation;
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
void rotate(uint3 id : SV_DispatchThreadID){
	float3 a = _TransformBuff[id.x].velocity;
	_TransformBuff[id.x].rotation.x = degrees(-asin(a.y / (length(a) + 1e-8)));
	_TransformBuff[id.x].rotation.y = degrees(atan2(a.x,a.z));
}
float3 boundary(uint3 id : SV_DispatchThreadID){
	float3 t = _TransformBuff[id.x].translate;
	float3 v = _TransformBuff[id.x].velocity;
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
float3 separate(uint3 id : SV_DispatchThreadID){
	if(_TransformBuff[id.x].separateCount > 0)
	{
		return normalize(_TransformBuff[id.x].separate / _TransformBuff[id.x].separateCount);
	}
	return ZERO;
}
float3 cohesion(uint3 id : SV_DispatchThreadID) {
	if(_TransformBuff[id.x].centerCount > 0)
	{
		float3 center = _TransformBuff[id.x].center / _TransformBuff[id.x].centerCount;
		return normalize(center - _TransformBuff[id.x].translate);
	}
	return ZERO;
}
float3 align(uint3 id : SV_DispatchThreadID) {
	if(_TransformBuff[id.x].velocitySumCount > 0 && length(_TransformBuff[id.x].velocitySum) > 0)
	{
		return normalize(_TransformBuff[id.x].velocitySum / _TransformBuff[id.x].velocitySumCount);
	}
	return ZERO;
}
float3 feed(uint3 id : SV_DispatchThreadID)
{
	float3 diff = _FeedPosition - _TransformBuff[id.x].translate;
	float dist = length(diff);
	if(dist < _FeedNeighborDistance)
	{
		return normalize(diff);
	}
	return ZERO;
}
void motion(uint3 id : SV_DispatchThreadID){
	float3 force
		= (separate(id)	* _SeparateWeight)
		+ (cohesion(id)	* _CohesionWeight)
		+ (align(id)	* _AlignWeight)
		+ (feed(id)		* _FeedWeight)
		+ (boundary(id)	* _BoundaryWeight);
	_TransformBuff[id.x].velocity += force * unity_DeltaTime.x;
	float mag = length(_TransformBuff[id.x].velocity);
	if(mag > _Speed){
		_TransformBuff[id.x].velocity *= (_Speed / mag);
	}
	if(mag == 0){
		_TransformBuff[id.x].velocity = float3(
			rand(id, rand(id, dot(_Time.x,_Time.y))),
			rand(id, rand(id, dot(_Time.y,_Time.z))),
			rand(id, rand(id, dot(_Time.z,_Time.w)))) * 10.0;
	}
	_TransformBuff[id.x].translate += _TransformBuff[id.x].velocity * unity_DeltaTime.x;
}
void finalize(uint3 id : SV_DispatchThreadID)
{
	_TransformBuff[id.x].separate = ZERO;
	_TransformBuff[id.x].separateCount = 0.0;
	_TransformBuff[id.x].center = ZERO;
	_TransformBuff[id.x].centerCount = 0.0;
	_TransformBuff[id.x].velocitySum = ZERO;
	_TransformBuff[id.x].velocitySumCount = 0.0;
}
void initialize(uint3 id : SV_DispatchThreadID){
	_TransformBuff[id.x].translate = (float3(
		rand(id, rand(id, dot(_Time.x,_Time.y))),
		rand(id, rand(id, dot(_Time.y,_Time.z))),
		rand(id, rand(id, dot(_Time.z,_Time.w)))) - 0.5) * _Bounds;
	_TransformBuff[id.x].velocity = (float3)0.0;
}
void forceCompute(uint3 id : SV_DispatchThreadID){
    float3 diff = _TransformBuff[id.x].translate - _TransformBuff[id.y].translate;
	float dist = length(diff);
	if (dist > 0.0 && dist < _SeparateNeighborDistance)
	{
		_TransformBuff[id.x].separate += (normalize(diff) / dist);
		_TransformBuff[id.x].separateCount++;
	}
	if (dist > 0.0 && dist < _CohesionNeighborDistance)
	{
		_TransformBuff[id.x].center += _TransformBuff[id.y].translate;
		_TransformBuff[id.x].centerCount++;
	}
	if (dist > 0.0 && dist < _AlignNeighborDistance)
	{
		_TransformBuff[id.x].velocitySum += _TransformBuff[id.y].velocity;
		_TransformBuff[id.x].velocitySumCount++;
	}
}
void boidsCompute(uint3 id : SV_DispatchThreadID){
    motion(id);
	rotate(id);
	finalize(id);
}