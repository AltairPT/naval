﻿using naval.Teams;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
public partial class NavalPlayer : Player
{
	private TimeSince timeSinceDropped;
	private TimeSince timeSinceJumpReleased;

	private DamageInfo lastDamage;

	// For vehicles
	[Net] public PawnController VehicleController { get; set; }
	[Net] public ICamera VehicleCamera { get; set; }
	[Net] public Entity Vehicle { get; set; }
	[Net] public ICamera MainCamera { get; set; }

	public ICamera LastCamera { get; set; }

	// time since last "FIRE!" scream by the player (when shooting cannons)
	public TimeSince timeSinceLastFireScream;

	public NavalPlayer()
	{
		Inventory = new Inventory( this );
	}
	public override void Spawn()
	{
		LastCamera = new FirstPersonCamera();
		base.Spawn();
	}

	public override void Respawn()
	{
		SetModel( "models/citizen/citizen.vmdl" );

		Controller = new WalkController();
		Animator = new StandardPlayerAnimator();
		Camera = LastCamera;

		if ( DevController is NoclipController )
		{
			DevController = null;
		}

		EnableAllCollisions = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;

		Dress();

		Inventory.Add( new PhysGun(), true );
		Inventory.Add( new GravGun() );
		Inventory.Add( new Tool() );
		Inventory.Add( new Flintlock() );
		Inventory.Add( new Cutlass() );
		//Inventory.Add( new Pistol() );
		//Inventory.Add( new Shotgun() );
		//Inventory.Add( new SMG() );
		//Inventory.Add( new Flashlight() );

		//this.Team = NavalGame.Instance.RoyalNavy;


		base.Respawn();
	}

	public override void OnKilled()
	{
		base.OnKilled();

		BecomeRagdollOnClient( Velocity, lastDamage.Flags, lastDamage.Position, lastDamage.Force, GetHitboxBone( lastDamage.HitboxIndex ) );
		LastCamera = Camera;
		Camera = new SpectateRagdollCamera();
		Controller = null;

		EnableAllCollisions = false;
		EnableDrawing = false;

		Inventory.DropActive();
		Inventory.DeleteContents();
	}

	public override void TakeDamage( DamageInfo info )
	{
		lastDamage = info;

		TookDamage( lastDamage.Flags, lastDamage.Position, lastDamage.Force );

		base.TakeDamage( info );
	}

	[ClientRpc]
	public void TookDamage( DamageFlags damageFlags, Vector3 forcePos, Vector3 force )
	{
	}

	public override PawnController GetActiveController()
	{
		if ( VehicleController != null ) return VehicleController;
		if ( DevController != null ) return DevController;

		return base.GetActiveController();
	}

	public override PawnAnimator GetActiveAnimator()
	{
		return base.GetActiveAnimator();
	}

	public ICamera GetActiveCamera()
	{
		if ( VehicleCamera != null ) return VehicleCamera;

		return MainCamera;
	}

	public override void Simulate( Client cl )
	{
		base.Simulate( cl );

		if ( Input.ActiveChild != null )
		{
			ActiveChild = Input.ActiveChild;
		}

		if ( LifeState != LifeState.Alive )
			return;

		TickPlayerUse();
		SimulateActiveChild( cl, ActiveChild );

		if ( Input.Pressed( InputButton.View ) )
		{
			if ( Camera is not FirstPersonCamera )
			{
				Camera = new FirstPersonCamera();
			}
			else
			{
				Camera = new ThirdPersonCamera();
			}
		}

		if ( Input.Pressed( InputButton.Drop ) )
		{
			var dropped = Inventory.DropActive();
			if ( dropped != null )
			{
				dropped.PhysicsGroup.ApplyImpulse( Velocity + EyeRot.Forward * 500.0f + Vector3.Up * 100.0f, true );
				dropped.PhysicsGroup.ApplyAngularImpulse( Vector3.Random * 100.0f, true );

				timeSinceDropped = 0;
			}
		}

		if ( Input.Released( InputButton.Jump ) )
		{
			if ( timeSinceJumpReleased < 0.3f )
			{
				Game.Current?.DoPlayerNoclip( cl );
			}

			timeSinceJumpReleased = 0;
		}

		if ( Input.Left != 0 || Input.Forward != 0 )
		{
			timeSinceJumpReleased = 1;
		}
	}

	public override void StartTouch( Entity other )
	{
		if ( timeSinceDropped < 1 ) return;

		base.StartTouch( other );
	}

	[ServerCmd( "inventory_current" )]
	public static void SetInventoryCurrent( string entName )
	{
		var target = ConsoleSystem.Caller.Pawn;
		if ( target == null ) return;

		var inventory = target.Inventory;
		if ( inventory == null )
			return;

		for ( int i = 0; i < inventory.Count(); ++i )
		{
			var slot = inventory.GetSlot( i );
			if ( !slot.IsValid() )
				continue;

			if ( !slot.ClassInfo.IsNamed( entName ) )
				continue;

			inventory.SetActiveSlot( i, false );

			break;
		}
	}

	// TODO

	//public override bool HasPermission( string mode )
	//{
	//	if ( mode == "noclip" ) return true;
	//	if ( mode == "devcam" ) return true;
	//	if ( mode == "suicide" ) return true;
	//
	//	return base.HasPermission( mode );
	//	}
}
