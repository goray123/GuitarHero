using System.Collections.Generic;
using UnityEngine;

// 난사는 본체와 기존 미사일이 끝난 뒤 단독으로 실행한다.
[DefaultExecutionOrder(120), DisallowMultipleComponent]
public class Boss1MissileBarrage : MonoBehaviour
{
    public enum Stage { Waiting, Preparing, Firing, Draining, Recovery }
    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float repeatInterval = 20f;
    [SerializeField, Min(0.01f)] private float preparationTime = 1f;
    [SerializeField, Min(1)] private int missileCount = 30;
    [SerializeField, Min(0.01f)] private float shotInterval = 0.2f;
    [SerializeField, Min(0.1f)] private float warningTime = 0.8f;
    [SerializeField, Min(0.01f)] private float descentTime = 0.35f;
    [SerializeField, Min(0.01f)] private float explosionTime = 0.2f;
    [SerializeField, Min(0f)] private float recoveryTime = 0.6f;
    [Header("Geometry (H)")]
    [SerializeField, Min(0f)] private float scatterH = 0.25f;
    [SerializeField, Min(0.01f)] private float explosionRadiusH = 0.4f;
    [SerializeField, Min(0.1f)] private float ascentSpeedH = 10f;
    [SerializeField] private Vector2 muzzleH = new Vector2(0.6f, 3.3f);
    [SerializeField] private LayerMask groundLayers = ~0;
    [Header("Preparation Art")]
    [SerializeField] private Sprite preparationSprite;
    [Tooltip("그림의 발바닥부터 머리까지 높이 / PPU. 발사기 회전 때문에 전체 바운드로 배율을 바꾸지 않음")]
    [SerializeField, Min(0.01f)] private float bodyReferenceHeight = 6.4f;
    public Stage CurrentStage { get; private set; }
    public bool IsActive => CurrentStage != Stage.Waiting;
    public int ShotsFired { get; private set; }
    private Boss1ProximityAttack owner;
    private Boss1AimedMissile aimed;
    private Boss1Health health;
    private Boss1MissileArt art;
    private Boss1AttackHitboxes damage;
    private SpriteRenderer pose;
    private Material lineMaterial;
    private AudioSource warningSource;
    private AudioClip warningClip;
    private float timer, nextShot, facing;
    private bool reserved;
    private float H => owner.Height * Mathf.Abs(transform.lossyScale.y);
    private readonly List<Rocket> rockets = new List<Rocket>();
    private sealed class Rocket {
        public SpriteRenderer visual;
        public LineRenderer warning;
        public Vector2 position, target, dropStart;
        public float sky, age;
        public int phase;
        public HashSet<PlayerMovement> hit = new HashSet<PlayerMovement>();
    }
    private void Awake() {
        owner=GetComponent<Boss1ProximityAttack>(); aimed=GetComponent<Boss1AimedMissile>();
        health=GetComponent<Boss1Health>(); art=GetComponent<Boss1MissileArt>(); damage=GetComponent<Boss1AttackHitboxes>();
        timer=repeatInterval;
        warningSource=gameObject.AddComponent<AudioSource>(); warningSource.playOnAwake=false; warningSource.spatialBlend=0f;
        var samples=new float[2205];
        for(int i=0;i<samples.Length;i++) samples[i]=Mathf.Sin(2f*Mathf.PI*880f*i/22050f)*Mathf.Sin(Mathf.PI*i/samples.Length)*.12f;
        warningClip=AudioClip.Create("Barrage Warning",samples.Length,1,22050,false); warningClip.SetData(samples,0);
    }
    private void FixedUpdate() {
        if(owner==null || !owner.isActiveAndEnabled || (health!=null && health.IsDead)) { Cancel(); return; }
        float dt=Time.fixedDeltaTime;
        if(CurrentStage==Stage.Waiting) {
            timer-=dt;
            if(timer<=0f) { reserved=true; owner.BarragePending=true; }
            if(!reserved || !owner.HasMissileTarget || owner.NextExclusiveAction!=Boss1ProximityAttack.ExclusiveAction.Barrage || !owner.ReadyForExclusiveAction) return;
            owner.BarrageActive=true; facing=owner.Facing;
            CurrentStage=Stage.Preparing; timer=preparationTime; ShotsFired=0; nextShot=0f;
        } else if(CurrentStage==Stage.Preparing) {
            timer-=dt;
            if(timer<=0f) CurrentStage=Stage.Firing;
        } else if(CurrentStage==Stage.Firing) {
            nextShot-=dt;
            while(nextShot<=0f && ShotsFired<missileCount) { Launch(); ShotsFired++; nextShot+=Mathf.Max(.01f,shotInterval); }
            if(ShotsFired>=missileCount) CurrentStage=Stage.Draining;
        }
        AdvanceRockets(dt);
        if(CurrentStage==Stage.Draining && rockets.Count==0) { CurrentStage=Stage.Recovery; timer=recoveryTime; }
        else if(CurrentStage==Stage.Recovery) {
            timer-=dt;
            if(timer<=0f) { owner.BarrageActive=false; owner.BarragePending=false; reserved=false;
                aimed?.DeferNewVolleys(1.5f); CurrentStage=Stage.Waiting; timer=repeatInterval; }
        }
    }
    private void Launch() {
        var r=new Rocket();
        r.position=(Vector2)transform.position+new Vector2((ShotsFired%2==0?-1:1)*muzzleH.x*H,muzzleH.y*H);
        var camera=Camera.main;
        float top=camera!=null ? camera.ViewportToWorldPoint(new Vector3(.5f,1f,Mathf.Abs(camera.transform.position.z-transform.position.z))).y : r.position.y+8f*H;
        r.sky=Mathf.Max(top+H,r.position.y+2f*H);
        r.visual=new GameObject("Barrage Missile").AddComponent<SpriteRenderer>(); r.visual.sortingOrder=24;
        rockets.Add(r);
    }
    private Vector2 GroundPoint(float x) {
        float y=owner.MissileTargetPoint.y+10f*H;
        var hits=Physics2D.RaycastAll(new Vector2(x,y),Vector2.down,50f*H,groundLayers);
        foreach(var hit in hits) if(!hit.collider.isTrigger && hit.collider.transform.root!=transform.root && hit.collider.GetComponentInParent<Damageable>()==null && hit.collider.GetComponentInParent<PlayerMovement>()==null) return hit.point;
        return new Vector2(x,GetComponent<Collider2D>().bounds.min.y);
    }
    private void AdvanceRockets(float dt) {
        for(int i=rockets.Count-1;i>=0;i--) {
            var r=rockets[i];
            if(r.phase==0) {
                r.position+=Vector2.up*(ascentSpeedH*H*dt);
                if(r.position.y>=r.sky) {
                    // 화면 밖에 도달한 미사일마다 당시 플레이어 위치로 착탄점을 결정.
                    r.target=GroundPoint(owner.MissileTargetPoint.x+Random.Range(-scatterH,scatterH)*H);
                    r.dropStart=new Vector2(r.target.x,r.sky); r.phase=1; r.age=0f;
                    r.warning=CreateWarning(r.target,explosionRadiusH*H);
                }
            } else if(r.phase==1) {
                r.age+=dt;
                float flight=Mathf.Min(descentTime,warningTime);
                float t=Mathf.Clamp01((r.age-(warningTime-flight))/Mathf.Max(.01f,flight));
                r.position=Vector2.Lerp(r.dropStart,r.target,t);
                if(r.age>=warningTime) { r.phase=2; r.age=0f; Destroy(r.warning.gameObject); r.warning=null; art?.PlayExplosion(r.target,explosionRadiusH*H); }
            } else {
                r.age+=dt;
                if(r.age>=explosionTime) { Release(r); rockets.RemoveAt(i); continue; }
            }
            if(r.phase==2) {
                foreach(var c in Physics2D.OverlapCircleAll(r.target,explosionRadiusH*H)) {
                    var player=c.GetComponentInParent<PlayerMovement>();
                    if(player!=null && player.isActiveAndEnabled && r.hit.Add(player)) damage.DeliverHit(player,c.ClosestPoint(r.target),Vector2.down,.06f,7);
                }
            }
            r.visual.enabled=r.phase!=2;
            if(r.visual.enabled && art!=null) art.PoseRocket(r.visual,r.position,r.phase==0?Vector2.up:Vector2.down);
        }
    }
    private LineRenderer CreateWarning(Vector2 point,float radius) {
        if(warningSource!=null) warningSource.PlayOneShot(warningClip);
        if(lineMaterial==null) lineMaterial=new Material(Shader.Find("Sprites/Default"));
        var line=new GameObject("Barrage Impact Warning").AddComponent<LineRenderer>();
        line.sharedMaterial=lineMaterial; line.useWorldSpace=true; line.loop=true; line.positionCount=48;
        line.startWidth=line.endWidth=.035f*H; line.startColor=line.endColor=new Color(1f,.15f,.05f,.8f); line.sortingOrder=23;
        for(int i=0;i<48;i++){float a=i*Mathf.PI*2f/48f;line.SetPosition(i,point+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius);}
        return line;
    }
    private void LateUpdate() {
        if(!IsActive || preparationSprite==null) { if(pose!=null) pose.enabled=false; return; }
        if(pose==null) { var child=new GameObject("Barrage Preparation Pose"); child.transform.SetParent(transform,false); pose=child.AddComponent<SpriteRenderer>(); pose.sortingOrder=5; }
        pose.sprite=preparationSprite; pose.enabled=true;
        float scale=3.45f*owner.Height/bodyReferenceHeight;
        pose.transform.localScale=new Vector3(scale*facing,scale,1f);
        var bodyVisual=GetComponent<Boss1SlamSpriteVisual>();
        if(bodyVisual!=null && bodyVisual.BodyRenderer!=null) bodyVisual.BodyRenderer.enabled=false;
        GetComponent<Boss1PrototypeModel>()?.SetBodyVisible(false);
    }
    private void Release(Rocket r) { if(r.visual!=null) Destroy(r.visual.gameObject); if(r.warning!=null) Destroy(r.warning.gameObject); }
    public void Cancel() {
        foreach(var r in rockets) Release(r); rockets.Clear();
        if(owner!=null) { owner.BarrageActive=false; owner.BarragePending=false; }
        reserved=false; CurrentStage=Stage.Waiting; timer=repeatInterval; if(pose!=null)pose.enabled=false;
    }
    private void OnDisable()=>Cancel();
    private void OnDestroy(){Cancel();if(pose!=null)Destroy(pose.gameObject);if(lineMaterial!=null)Destroy(lineMaterial);if(warningClip!=null)Destroy(warningClip);if(warningSource!=null)Destroy(warningSource);}
}
