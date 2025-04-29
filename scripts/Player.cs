using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[ExportGroup("Movement Settings")]
	[Export]
	public float MaxSpeed { get; set; } = 150.0f;
	
	[Export]
	public float AccelerationTime { get; set; } = 0.2f;
	
	[Export]
	public float DecelerationTime { get; set; } = 0.1f;
	
	[Export]
	public float StartSpeedMultiplier { get; set; } = 0.3f;
	
	[Export]
	public float DirectionChangeMultiplier { get; set; } = 1.5f;
	
	[Export]
	public float MinimumMoveThreshold { get; set; } = 0.1f;

	[ExportGroup("Animation Settings")]
	[Export]
	public float MinSpeedScale { get; set; } = 0.7f;
	
	[Export]
	public float MaxSpeedScale { get; set; } = 1.3f;

	private enum FacingDirection
	{
		Left,
		Right,
		Up,
		Down
	}

	private float _acceleration;
	private float _friction;
	private AnimatedSprite2D _animatedSprite;
	private Vector2 _velocity;
	private Vector2 _lastInput;
	private FacingDirection _currentDirection = FacingDirection.Down; // Default facing down
	private float _currentSpeedMultiplier;

	public override void _Ready()
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		
		_acceleration = MaxSpeed / AccelerationTime;
		_friction = MaxSpeed / DecelerationTime;
		
		_currentSpeedMultiplier = StartSpeedMultiplier;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		input = NormalizeInput(input);
		
		HandleMovement(input, delta);
		UpdateAnimation(input);
		MoveAndSlide();
		
		_lastInput = input;
	}

	private Vector2 NormalizeInput(Vector2 input)
	{
		return input.LengthSquared() > 1.0f ? input.Normalized() : input;
	}

	private void HandleMovement(Vector2 input, double delta)
	{
		if (input != Vector2.Zero)
		{
			float accelerationModifier = 1.0f;

			if (_lastInput != Vector2.Zero && input.Dot(_lastInput) < 0)
			{
				accelerationModifier = DirectionChangeMultiplier;
			}

			_currentSpeedMultiplier = Mathf.Min(
				1.0f,
				_currentSpeedMultiplier + (float)delta * (1.0f - StartSpeedMultiplier) / AccelerationTime
			);

			Vector2 targetVelocity = input * MaxSpeed * _currentSpeedMultiplier;
			_velocity = _velocity.MoveToward(
				targetVelocity,
				_acceleration * accelerationModifier * (float)delta
			);
			
			UpdateFacingDirection(input);
		}
		else
		{
			_currentSpeedMultiplier = StartSpeedMultiplier;
			
			_velocity = _velocity.MoveToward(
				Vector2.Zero,
				_friction * (float)delta
			);
		}

		if (_velocity.Length() < MinimumMoveThreshold)
		{
			_velocity = Vector2.Zero;
		}

		Velocity = _velocity;
	}

	private void UpdateFacingDirection(Vector2 input)
	{
		// Maybe add diagonal animations later.
		float absX = Mathf.Abs(input.X);
		float absY = Mathf.Abs(input.Y);

		if (absX > absY && absX > MinimumMoveThreshold)
		{
			_currentDirection = input.X > 0 ? FacingDirection.Right : FacingDirection.Left;
		}
		else if (absY > MinimumMoveThreshold)
		{
			_currentDirection = input.Y > 0 ? FacingDirection.Down : FacingDirection.Up;
		}
	}

	private void UpdateAnimation(Vector2 input)
	{
		float speedRatio = _velocity.Length() / MaxSpeed;
		
		if (speedRatio > MinimumMoveThreshold)
		{
			string animationName = _currentDirection switch
			{
				FacingDirection.Left => "walk_side",
				FacingDirection.Right => "walk_side",
				FacingDirection.Up => "walk_up",
				FacingDirection.Down => "walk_down",
				_ => "walk_down"
			};

			_animatedSprite.Play(animationName);
			
			_animatedSprite.FlipH = _currentDirection == FacingDirection.Left;
			
			float targetSpeedScale = Mathf.Lerp(MinSpeedScale, MaxSpeedScale, speedRatio);
			_animatedSprite.SpeedScale = Mathf.Lerp(
				_animatedSprite.SpeedScale,
				targetSpeedScale,
				0.2f
			);
		}
		else
		{
			string idleAnimation = _currentDirection switch
			{
				FacingDirection.Left => "idle_side",
				FacingDirection.Right => "idle_side",
				FacingDirection.Up => "idle_up",
				FacingDirection.Down => "idle_down",
				_ => "idle_down"
			};

			_animatedSprite.Play(idleAnimation);
			_animatedSprite.FlipH = _currentDirection == FacingDirection.Left;
			_animatedSprite.SpeedScale = 1.0f;
		}
	}
	public override void _Process(double delta)
	{
		if (OS.IsDebugBuild())
		{
			string debugInfo = $"Speed: {_velocity.Length():F1}\n" +
							 $"Direction: {_currentDirection}\n" +
							 $"Multiplier: {_currentSpeedMultiplier:F2}\n" +
							 $"Animation Scale: {_animatedSprite.SpeedScale:F2}";
			
			var debugLabel = GetNodeOrNull<Label>("DebugLabel");
			if (debugLabel != null)
			{
				debugLabel.Text = debugInfo;
			}
		}
	}
}
