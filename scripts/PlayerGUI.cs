using Godot;
using System;

public partial class PlayerGUI : Control
{
    private Label healthLabel;
    private Player player;

    public override void _Ready()
    {
        healthLabel = GetNode<Label>("BottomLeft/HBoxContainer/HealthLabel");
        
        // Find the player node safely using CallDeferred
        CallDeferred(nameof(ConnectToPlayer));
    }
    
    private void ConnectToPlayer()
    {
        // Find the player node
        player = GetNode<Player>("/root/TestLevel/Player");
        if (player != null && IsInstanceValid(player))
        {
            // Connect to player's health changed signal
            player.HealthChanged += OnPlayerHealthChanged;
            
            // Initialize health display
            OnPlayerHealthChanged(player.CurrentHealth, player.MaxHealth);
        }
    }

    private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
    {
        if (healthLabel != null && IsInstanceValid(healthLabel))
        {
            healthLabel.Text = $"{currentHealth}/{maxHealth}";
        }
    }

    public override void _ExitTree()
    {
        if (player != null && IsInstanceValid(player))
        {
            player.HealthChanged -= OnPlayerHealthChanged;
        }
        
        base._ExitTree();
    }
} 