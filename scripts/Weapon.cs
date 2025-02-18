using Godot;
using System;

public partial class Weapon : Node2D
{
    [Export] public float SwingSpeed = 10.0f;
    [Export] public float SwingAngle = 120.0f;
    [Export] public float ReturnSpeed = 15.0f;
    
    private Sprite2D sprite;
    private Marker2D bottomPivot;
    private Marker2D centerPivot;
    private bool isSwinging = false;
    private float currentAngle = 0.0f;
    private float targetAngle = 0.0f;
    private bool useBottomPivot = true;
    
    public override void _Ready()
    {
        sprite = GetNode<Sprite2D>("Sprite2D");
        bottomPivot = GetNode<Marker2D>("BottomPivot");
        centerPivot = GetNode<Marker2D>("CenterPivot");
        
        // Initialize weapon position
        ResetWeapon();
    }
    
    public override void _Process(double delta)
    {
        if (isSwinging)
        {
            // Update weapon rotation
            float angleDiff = targetAngle - currentAngle;
            float rotationAmount = SwingSpeed * (float)delta;
            
            if (Math.Abs(angleDiff) < rotationAmount)
            {
                currentAngle = targetAngle;
                isSwinging = false;
                
                // If we reached the target angle during attack, start returning
                if (targetAngle != 0)
                {
                    ReturnToIdle();
                }
            }
            else
            {
                currentAngle += Math.Sign(angleDiff) * rotationAmount;
            }
            
            // Apply rotation around the current pivot point
            var pivotPoint = useBottomPivot ? bottomPivot.Position : centerPivot.Position;
            Rotation = Mathf.DegToRad(currentAngle);
            
            // If using center pivot, adjust position to maintain the bottom point
            if (!useBottomPivot)
            {
                // Additional position adjustment might be needed based on your specific weapon
            }
        }
    }
    
    public void StartAttackSwing()
    {
        if (!isSwinging)
        {
            isSwinging = true;
            targetAngle = -SwingAngle; // Negative for clockwise rotation
            useBottomPivot = true;
        }
    }
    
    private void ReturnToIdle()
    {
        isSwinging = true;
        targetAngle = 0;
        useBottomPivot = false; // Use center pivot for return animation
        SwingSpeed = ReturnSpeed;
    }
    
    public void ResetWeapon()
    {
        isSwinging = false;
        currentAngle = 0;
        targetAngle = 0;
        Rotation = 0;
        SwingSpeed = 10.0f; // Reset to default swing speed
    }
    
    public bool IsSwinging()
    {
        return isSwinging;
    }
} 