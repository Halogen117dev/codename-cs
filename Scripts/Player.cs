using Godot;
using System;

public partial class Player : CharacterBody3D
{
    const float Speed = 4.1f;
    const float CrouchSpeed = 0.12f; //speed at which player dips down to the crouch position OBVI
                                     //using multipliers instead because FUCK THIS
    private float CrouchMult = 0.65f;
    private float SprintMult = 1.5f;
    private float CrouchDepthMult = 0.5f;
    private float AerialDampMult = 0.035f;          //damping of momentum when in air AND NO INPUT
    private float AerialMoveDampMult = 0.15f;		//damping of momentum when in air AND INPUT

    //crouch depth calculated at ready
    private float CrouchDepth = 0.0f;
    private float StandColDepth= 0.0f;
    private float StandDepth = 0.0f;

    const float Gravity = 17.4f;
    const float JumpForce = 7.0f;

    //To make movement not as snappy
    const float Acceleration = 26;
    const float Deceleration = 24;


    //different mouse sensitivity in concept
    const float HorSensitivity = 0.001f;
    const float VerSensitivity = 0.001f;



    private Node3D Head;
    private Camera3D Camera;
    private CollisionShape3D BodyCollider;
    private MeshInstance3D BodyMesh;

    private Vector3 LocalVelocity = Vector3.Zero;

    private float mouseDX = 0.0f;
    private float mouseDY = 0.0f;

    private bool IsSprinting = false;
    private bool IsCrouching = false;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GD.Print("hello world");

        Head = GetNode<Node3D>("Head");
        Camera = GetNode<Camera3D>("Head/Camera3D");
        BodyCollider = GetNode<CollisionShape3D>("BodyCollider");
        BodyMesh = GetNode<MeshInstance3D>("BodyMesh");

        //Input.MouseMode = Input.MouseModeEnum.Captured;

        CrouchDepth = Head.Position.Y * CrouchDepthMult;
        StandColDepth = BodyCollider.Position.Y;
        StandDepth = Head.Position.Y;
    }


    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion)
        {
            InputEventMouseMotion mouseMotion = @event as InputEventMouseMotion;
            this.RotateY(-mouseMotion.Relative.X * HorSensitivity);
            Head.RotateX(-mouseMotion.Relative.Y * VerSensitivity);

            Vector3 headRot = Head.Rotation;
            headRot.X = Mathf.Clamp(headRot.X, Mathf.DegToRad(-80.0f), Mathf.DegToRad(85.0f));
            Head.Rotation = headRot;
        }
    }


    private void CrouchBehavior()
    {
        if (IsCrouching)
        {
            Vector3 headPos = Head.Position;
            headPos.Y = Mathf.Lerp(headPos.Y, CrouchDepth, CrouchSpeed);            
            //Head.Position = headPos;

            Vector3 bodyScale = BodyCollider.Scale;
            bodyScale.Y = Mathf.Lerp(BodyCollider.Scale.Y, CrouchDepth, CrouchSpeed);
            BodyCollider.Scale = bodyScale;

            Vector3 bodyPos = BodyCollider.Position;
            bodyPos.Y = Mathf.Lerp(BodyCollider.Position.Y, 1.0f + CrouchDepth, CrouchSpeed);
            BodyCollider.Position = bodyPos;
        }
        else
        {
            Vector3 headPos = Head.Position;
            headPos.Y = Mathf.Lerp(headPos.Y, StandDepth, CrouchSpeed);
            //Head.Position = headPos;

            Vector3 bodyScale = BodyCollider.Scale;
            bodyScale.Y = Mathf.Lerp(BodyCollider.Scale.Y, 1.0f, CrouchSpeed);
            BodyCollider.Scale = bodyScale;

            Vector3 bodyPos = BodyCollider.Position;
            bodyPos.Y = Mathf.Lerp(BodyCollider.Position.Y, StandColDepth, CrouchSpeed);
            BodyCollider.Position = bodyPos;
        }

    }

    public override void _PhysicsProcess(double delta)
    {
        if (Input.IsActionJustPressed("Quit"))
        {
            GetTree().Quit();
        }

        float dt = (float)delta;

        //fancy logic to denote whether player must be sprinting or not
        if (Input.IsActionJustPressed("Sprint") && !IsCrouching)
        {
            //makes it so that one click sprint, another click unsprint
            if (IsSprinting)
            {
                IsSprinting = false;
            }
            else
            {
                IsSprinting = true;
            }
        }
        if (Input.IsActionPressed("Crouch") && !IsSprinting)
        {
            IsCrouching = true;
        }
        else
        {
            IsCrouching = false;
        }


        Vector2 inputDir = Input.GetVector("Left", "Right", "Forward", "Backward");
        Vector3 direction = (GlobalTransform.Basis * new Vector3(inputDir.X, 0.0f, inputDir.Y)).Normalized();

        if (direction != Vector3.Zero)
        {
            float speedMult = IsCrouching ? CrouchMult : (IsSprinting ? SprintMult : 1.0f);
            float aerialDamping = AerialMoveDampMult;
            if (IsOnFloor())
            {
                aerialDamping = 1.0f;
            }

            LocalVelocity.X = Mathf.MoveToward(LocalVelocity.X, direction.X * Speed * speedMult, Acceleration * aerialDamping * dt);
            LocalVelocity.Z = Mathf.MoveToward(LocalVelocity.Z, direction.Z * Speed * speedMult, Acceleration * aerialDamping * dt);
        }
        else
        {
            float floorDamping = 1.0f;
            if (!IsOnFloor())
            {
                floorDamping = AerialDampMult;
            }
            IsSprinting = false;

            LocalVelocity.X = Mathf.MoveToward(LocalVelocity.X, 0.0f, Deceleration * floorDamping * dt);
            LocalVelocity.Z = Mathf.MoveToward(LocalVelocity.Z, 0.0f, Deceleration * floorDamping * dt);
        }


        if (!IsOnFloor())
        {
            LocalVelocity.Y -= Gravity * dt;
        }
        else if (Input.IsActionJustPressed("Jump"))
        {
            LocalVelocity.Y = JumpForce;
        }


        Velocity = LocalVelocity;

        CrouchBehavior();
        MoveAndSlide();
    }
}
