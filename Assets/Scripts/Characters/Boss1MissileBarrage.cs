using System.Collections.Generic;
using UnityEngine;

// 난사는 본체 패턴이며 패시브 조준 미사일과 동시에 실행될 수 있다.
[DefaultExecutionOrder(120), DisallowMultipleComponent]
public class Boss1MissileBarrage : MonoBehaviour
{
    public enum Stage { Waiting, Preparing, Firing, Draining, Recovery }
    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float repeatInterval = 20f;
    [SerializeField, Min(0.01f)] private float preparationTime = 1f;
    [SerializeField, Min(1)] private int missileCount = 30;
    [SerializeField, Min(0.01f)] private float shotInterval = 0.2f;
    [SerializeField, Min(0.01f)] private float explosionTime = 0.2f;
    [Tooltip("후딜은 이 값에 1.2배를 적용합니다.")]
    [SerializeField, Min(0f)] private float recoveryTime = 0.6f;
    [Header("Geometry (H)")]
    [SerializeField, Min(0f)] private float scatterH = 0.5f;
    [SerializeField, Min(0.01f)] private float explosionRadiusH = 0.4f;
    [SerializeField, Min(0.1f)] private float flightSpeedH = 10f;
    [SerializeField] private Vector2 muzzleH = new Vector2(0.6f, 3.3f);
    [Header("Preparation Art")]
    [SerializeField] private Sprite preparationSprite;
    [Tooltip("그림의 발바닥부터 머리까지 높이 / PPU. 발사기 회전 때문에 전체 바운드로 배율을 바꾸지 않음")]
    [SerializeField, Min(0.01f)] private float bodyReferenceHeight = 6.4f;
    public Stage CurrentStage { get; private set; }
    public bool IsActive => CurrentStage != Stage.Waiting;
    public int ShotsFired { get; private set; }
    private Boss1ProximityAttack owner;
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
        public Vector2 position, target, direction;
        public float age;
        public bool exploding;
        public HashSet<PlayerMovement> hit = new HashSet<PlayerMovement>();
    }
    private void Awake() {
        owner=GetComponent<Boss1ProximityAttack>();
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
            while(owner.HasMissileTarget && nextShot<=0f && ShotsFired<missileCount) { Launch(); ShotsFired++; nextShot+=Mathf.Max(.01f,shotInterval); }
            if(!owner.HasMissileTarget || ShotsFired>=missileCount) CurrentStage=Stage.Draining;
        }
        AdvanceRockets(dt);
        if(CurrentStage==Stage.Draining && rockets.Count==0) { CurrentStage=Stage.Recovery; timer=recoveryTime * 1.2f; }
        else if(CurrentStage==Stage.Recovery) {
            timer-=dt;
            if(timer<=0f) { owner.BarrageActive=false; owner.BarragePending=false; reserved=false;
                CurrentStage=Stage.Waiting; timer=repeatInterval; }
        }
    }

    private void Launch()
    {
        var rocket = new Rocket();
        float muzzleSide = ShotsFired % 2 == 0 ? -1f : 1f;
        rocket.position = (Vector2)transform.position + new Vector2(muzzleSide * muzzleH.x, muzzleH.y) * H;
        rocket.target = owner.MissileTargetPoint + Random.insideUnitCircle * (scatterH * H);
        rocket.direction = (rocket.target - rocket.position).normalized;
        rocket.visual = new GameObject("Barrage Missile").AddComponent<SpriteRenderer>();
        rocket.visual.sortingOrder = 24;
        rocket.warning = CreateWarning(rocket.target, explosionRadiusH * H);
        rockets.Add(rocket);
    }

    private void AdvanceRockets(float dt)
    {
        for (int i = rockets.Count - 1; i >= 0; i--)
        {
            var rocket = rockets[i];
            if (!rocket.exploding)
            {
                Vector2 next = Vector2.MoveTowards(rocket.position, rocket.target, flightSpeedH * H * dt);
                bool arrived = next.Equals(rocket.target);
                rocket.position = next;
                if (!arrived)
                {
                    art?.PoseRocket(rocket.visual, next, rocket.direction);
                    continue;
                }
                rocket.exploding = true;
                rocket.visual.enabled = false;
                Destroy(rocket.warning.gameObject);
                rocket.warning = null;
                art?.PlayExplosion(next, explosionRadiusH * H);
            }
            else
            {
                rocket.age += dt;
                if (rocket.age >= explosionTime)
                {
                    Release(rocket);
                    rockets.RemoveAt(i);
                    continue;
                }
            }
            foreach (var collider in Physics2D.OverlapCircleAll(rocket.position, explosionRadiusH * H))
            {
                var player = collider.GetComponentInParent<PlayerMovement>();
                if (player != null && player.isActiveAndEnabled && rocket.hit.Add(player))
                    damage.DeliverHit(player, collider.ClosestPoint(rocket.position), rocket.direction, 0.06f, 7);
            }
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
