using Godot;
using System;

public partial class DeathScreen : Control
{
    private Button restartButton;
    private Label messageLabel;
    private ColorRect background;
    private float fadeInDuration = 0.5f;
    private float currentFadeTime = 0;
    private bool isFading = false;

    public override void _Ready()
    {
        // Get node references
        restartButton = GetNode<Button>("CenterContainer/VBoxContainer/RestartButton");
        messageLabel = GetNode<Label>("CenterContainer/VBoxContainer/Label");
        background = GetNode<ColorRect>("ColorRect");
        
        // Set up initial state
        Hide();
        SetProcess(false);
        background.Modulate = new Color(1, 1, 1, 0);
        messageLabel.Modulate = new Color(1, 1, 1, 0);
        restartButton.Modulate = new Color(1, 1, 1, 0);
        
        // Connect signals
        restartButton.Pressed += OnRestartButtonPressed;
    }

    public override void _Process(double delta)
    {
        if (isFading)
        {
            currentFadeTime += (float)delta;
            float progress = Mathf.Clamp(currentFadeTime / fadeInDuration, 0, 1);
            
            // Fade in background first
            background.Modulate = new Color(1, 1, 1, progress * 0.588235f);
            
            // Start fading in text and button after a slight delay
            if (progress > 0.3f)
            {
                float textProgress = Mathf.Clamp((progress - 0.3f) / 0.7f, 0, 1);
                messageLabel.Modulate = new Color(1, 1, 1, textProgress);
                restartButton.Modulate = new Color(1, 1, 1, textProgress);
            }
            
            if (progress >= 1.0f)
            {
                isFading = false;
                SetProcess(false);
            }
        }
    }

    public void Show()
    {
        // Reset fade state
        currentFadeTime = 0;
        isFading = true;
        SetProcess(true);
        
        // Make control visible but with transparent elements
        Visible = true;
        background.Modulate = new Color(1, 1, 1, 0);
        messageLabel.Modulate = new Color(1, 1, 1, 0);
        restartButton.Modulate = new Color(1, 1, 1, 0);
        
        // Pause the game
        GetTree().Paused = true;
    }

    private void OnRestartButtonPressed()
    {
        // Unpause and reload
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }

    public override void _ExitTree()
    {
        if (restartButton != null)
        {
            restartButton.Pressed -= OnRestartButtonPressed;
        }
    }
} 