using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
    [Export] public float PatrolSpeed = 100.0f;
    [Export] public float PatrolDistance = 200.0f;
    [Export] public int Damage = 20;
    [Export] public float AttackCooldown = 1.0f;
    [Export] public int MaxHealth = 100;
    
    private int currentHealth;
    private Vector2 startPosition;
    private bool movingRight = true;
    private float attackCooldownTimer = 0.0f;
    private AnimationPlayer animationPlayer;
    private Sprite2D sprite;
    private ProgressBar healthBar;
    
    // Get the gravity from the project settings to be synced with RigidBody nodes
    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    
    public override void _Ready()
    {
        startPosition = Position;
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        sprite = GetNode<Sprite2D>("Sprite2D");
        healthBar = GetNode<ProgressBar>("HealthBar");
        
        // Initialize health
        currentHealth = MaxHealth;
        UpdateHealthBar();
        
        // Start with idle animation
        if (animationPlayer.HasAnimation("idle"))
            animationPlayer.Play("idle");
    }
    
    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;
        
        // Add gravity
        if (!IsOnFloor())
            velocity.Y += gravity * (float)delta;
            
        // Update attack cooldown
        if (attackCooldownTimer > 0)
            attackCooldownTimer -= (float)delta;
            
        // Handle patrol movement
        float distanceFromStart = Position.X - startPosition.X;
        
        if (movingRight && distanceFromStart >= PatrolDistance)
        {
            movingRight = false;
            sprite.FlipH = false;
        }
        else if (!movingRight && distanceFromStart <= 0)
        {
            movingRight = true;
            sprite.FlipH = true;
        }
        
        velocity.X = (movingRight ? 1 : -1) * PatrolSpeed;
        
        Velocity = velocity;
        MoveAndSlide();
    }
    
    private void OnBodyEntered(Node2D body)
    {
        if (body is Player player && attackCooldownTimer <= 0)
        {
            player.TakeDamage(Damage);
            attackCooldownTimer = AttackCooldown;
        }
    }
    
    public void TakeDamage(int damage)
    {
        currentHealth = Math.Max(0, currentHealth - damage);
        UpdateHealthBar();
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.Value = (float)currentHealth / MaxHealth * 100;
        }
    }
    
    private void Die()
    {
        // Ensure we clean up properly before removing
        if (healthBar != null && IsInstanceValid(healthBar))
        {
            healthBar.QueueFree();
        }
        
        // Queue the enemy for removal
        QueueFree();
    }

    public override void _ExitTree()
    {
        // Clean up any remaining resources
        if (healthBar != null && IsInstanceValid(healthBar))
        {
            healthBar.QueueFree();
        }
        
        base._ExitTree();
    }
} 