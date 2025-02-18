using Godot;
using System;

public partial class Player : CharacterBody2D
{
	// Movement properties
	[Export] public float MoveSpeed = 300.0f;
	[Export] public float JumpVelocity = -400.0f;
	[Export] public float DoubleJumpVelocity = -300.0f;
	
	// Combat properties
	[Export] public int SwordDamage = 25;
	[Export] public float AttackCooldown = 0.5f;
	[Export] public float InvincibilityDuration = 1.0f;
	[Export] private int maxHealth = 100;
	
	// State variables
	private bool canDoubleJump = false;
	private bool isAttacking = false;
	private bool isBlocking = false;
	private bool isDead = false;
	private bool isInvincible = false;
	private float attackCooldownTimer = 0.0f;
	private float invincibilityTimer = 0.0f;
	private int currentHealth;
	private string currentState = "idle";
	private bool isFacingRight = true;
	
	// Node references
	private AnimationPlayer animationPlayer;
	private Sprite2D sprite;
	private CollisionShape2D collisionShape;
	private Area2D hazardDetector;
	private Area2D attackHitbox;
	private Weapon weapon;
	private DeathScreen deathScreen;
	
	// Signals
	[Signal]
	public delegate void HealthChangedEventHandler(int newHealth, int maxHealth);
	[Signal]
	public delegate void PlayerDiedEventHandler();
	
	// Get the gravity from the project settings to be synced with RigidBody nodes
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
	
	// Properties
	public int CurrentHealth => currentHealth;
	public int MaxHealth => maxHealth;
	
	public override void _Ready()
	{
		// Initialize health
		currentHealth = maxHealth;
		EmitSignal(SignalName.HealthChanged, currentHealth, maxHealth);
		
		// Get node references
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		sprite = GetNode<Sprite2D>("Sprite2D");
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		hazardDetector = GetNode<Area2D>("HazardDetector");
		attackHitbox = GetNode<Area2D>("AttackHitbox");
		weapon = GetNode<Weapon>("Weapon");
		
		// Try to get death screen reference
		var canvasLayer = GetNode<CanvasLayer>("/root/TestLevel/CanvasLayer");
		if (canvasLayer != null)
		{
			deathScreen = canvasLayer.GetNode<DeathScreen>("DeathScreen");
		}
		
		// Connect signals
		animationPlayer.AnimationFinished += OnAnimationFinished;
		hazardDetector.AreaEntered += OnHazardAreaEntered;
		if (attackHitbox != null)
		{
			attackHitbox.Monitoring = false;
		}
		
		// Start with idle animation
		PlayAnimation("idle");
	}
	
	public override void _PhysicsProcess(double delta)
	{
		if (isDead) return;
		
		HandleTimers(delta);
		HandleMovement(delta);
		HandleCombat();
	}
	
	private void HandleTimers(double delta)
	{
		// Update attack cooldown
		if (attackCooldownTimer > 0)
		{
			attackCooldownTimer -= (float)delta;
		}
		
		// Update invincibility
		if (isInvincible)
		{
			invincibilityTimer -= (float)delta;
			if (invincibilityTimer <= 0)
			{
				isInvincible = false;
				sprite.Modulate = new Color(1, 1, 1, 1);
			}
		}
	}
	
	private void HandleMovement(double delta)
	{
		Vector2 velocity = Velocity;
		
		// Add gravity
		if (!IsOnFloor())
		{
			velocity.Y += gravity * (float)delta;
			if (velocity.Y > 0)
				ChangeState("jump");
		}
		
		// Handle jump
		if (Input.IsActionJustPressed("jump"))
		{
			if (IsOnFloor())
			{
				velocity.Y = JumpVelocity;
				canDoubleJump = true;
				ChangeState("jump");
			}
			else if (canDoubleJump)
			{
				velocity.Y = DoubleJumpVelocity;
				canDoubleJump = false;
				ChangeState("jump");
			}
		}
		
		// Handle horizontal movement
		float direction = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
		
		if (!isAttacking)
		{
			velocity.X = direction * MoveSpeed;
			
			// Update facing direction (reversed logic)
			if (direction != 0)
			{
				isFacingRight = direction > 0;
				sprite.FlipH = isFacingRight;
			}
		}
		
		// Update animation state
		if (IsOnFloor() && !isAttacking && !isBlocking)
		{
			if (Math.Abs(velocity.X) > 0.1f)
				ChangeState("run");
			else
				ChangeState("idle");
		}
		
		Velocity = velocity;
		MoveAndSlide();
	}
	
	private void HandleCombat()
	{
		// Handle sword attack
		if (Input.IsActionJustPressed("attack") && attackCooldownTimer <= 0 && !isAttacking)
		{
			Attack();
		}
		
		// Handle blocking
		if (Input.IsActionPressed("block") && !isAttacking)
		{
			Block();
		}
		else if (Input.IsActionJustReleased("block"))
		{
			StopBlocking();
		}
	}
	
	private void Attack()
	{
		if (isBlocking || isDead) return;
		
		isAttacking = true;
		attackCooldownTimer = AttackCooldown;
		
		// Start weapon swing
		if (weapon != null)
		{
			weapon.StartAttackSwing();
		}
		
		// Enable attack hitbox
		if (attackHitbox != null)
		{
			attackHitbox.Monitoring = true;
			
			// Schedule disabling the hitbox
			GetTree().CreateTimer(0.2f).Timeout += () =>
			{
				attackHitbox.Monitoring = false;
			};
		}
		
		ChangeState("attack");
	}
	
	private void Block()
	{
		if (isAttacking || isDead) return;
		
		isBlocking = true;
		ChangeState("block");
	}
	
	private void StopBlocking()
	{
		isBlocking = false;
		ChangeState("idle");
	}
	
	private void ChangeState(string newState)
	{
		if (currentState == newState) return;
		if (isDead) return;
		
		// Don't change state if in the middle of an attack animation
		if (isAttacking && currentState == "attack") return;
		
		currentState = newState;
		PlayAnimation(currentState);
	}
	
	private void PlayAnimation(string animName)
	{
		if (animationPlayer.HasAnimation(animName))
		{
			animationPlayer.Play(animName);
		}
		else
		{
			animationPlayer.Play("idle");
		}
	}
	
	private void OnAnimationFinished(StringName animName)
	{
		if (animName == "attack")
		{
			isAttacking = false;
			if (weapon != null && !weapon.IsSwinging())
			{
				weapon.ResetWeapon();
			}
		}
		else if (animName == "death")
		{
			// Use CallDeferred to safely remove the player
			CallDeferred(nameof(CleanupAndRemove));
		}
	}
	
	public void TakeDamage(int damage)
	{
		if (isDead || isInvincible) return;
		
		if (isBlocking)
		{
			// Reduce damage when blocking
			damage = (int)(damage * 0.2f);
		}
		
		currentHealth -= damage;
		EmitSignal(SignalName.HealthChanged, currentHealth, MaxHealth);
		
		// Add invincibility frames
		isInvincible = true;
		invincibilityTimer = InvincibilityDuration;
		sprite.Modulate = new Color(1, 1, 1, 0.5f);
		
		if (currentHealth <= 0)
		{
			Die();
		}
	}
	
	private void Die()
	{
		if (isDead) return;
		
		isDead = true;
		isAttacking = false;
		isBlocking = false;
		
		EmitSignal(SignalName.PlayerDied);
		
		// Disable collision safely using CallDeferred
		CallDeferred(nameof(DisableCollision));
		
		// Play death animation
		PlayAnimation("death");
		
		// Show death screen safely using CallDeferred
		if (deathScreen != null && !deathScreen.IsQueuedForDeletion())
		{
			CallDeferred(nameof(ShowDeathScreen));
		}
		
		GD.Print("Player died!");
	}
	
	private void ShowDeathScreen()
	{
		if (deathScreen != null && IsInstanceValid(deathScreen))
		{
			deathScreen.Show();
		}
	}
	
	private void DisableCollision()
	{
		if (collisionShape != null && IsInstanceValid(collisionShape))
		{
			collisionShape.SetDeferred("disabled", true);
		}
	}
	
	private void CleanupAndRemove()
	{
		// Safely disconnect signals
		if (animationPlayer != null && IsInstanceValid(animationPlayer))
		{
			animationPlayer.AnimationFinished -= OnAnimationFinished;
		}
		
		if (hazardDetector != null && IsInstanceValid(hazardDetector))
		{
			hazardDetector.AreaEntered -= OnHazardAreaEntered;
		}
		
		// Queue free the node
		QueueFree();
	}
	
	public override void _ExitTree()
	{
		// Ensure we're not in the middle of any animations or state changes
		isDead = true;
		isAttacking = false;
		isBlocking = false;
		
		CleanupAndRemove();
		base._ExitTree();
	}
	
	private void OnHazardAreaEntered(Area2D area)
	{
		if (area.IsInGroup("hazard"))
		{
			Die();
		}
	}
	
	private void OnAttackHitboxEntered(Node2D body)
	{
		if (body is Enemy enemy)
		{
			enemy.TakeDamage(SwordDamage);
		}
	}
} 
