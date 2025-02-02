﻿using Sandbox;
using Sandbox.Tools;
using System.Runtime.CompilerServices;

[Library( "nvl_blackpowder_cannon", Title = "Blackpowder Cannon", Spawnable = true )]
public partial class BlackpowderCannonEntity : Prop, IUse
{
	public float WickTime = 1.2f; //(seconds) how long the wick burns before shooting the cannonball
	public float ReloadTime = 4f; //(seconds) how long it takes to reload the cannon
	public float RecoilForce = 250f; //(hu) how much kickback should the cannon recieve after each shoot
	public float ProjectileVelocity = 2400f; //(hu) how much velocity should be applied to cannon ball upon firing
	public bool IsReloaded = true;
	public Sound WickSound = new Sound();
	public Entity CannonPlatformParent = null;
	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/naval/props/props/cannon_barrel.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic, false );
	}

	public bool IsUsable( Entity user )
	{
		return true;
	}

	public bool OnUse( Entity user )
	{

		if ( IsReloaded == true )
		{
			//TEMP set owner is here!
			Owner = user;

			//Player screaming FIRE!
			if ( (user as NavalPlayer).timeSinceLastFireScream > 8f ) 
			{
				(user as NavalPlayer).timeSinceLastFireScream = 0;
				Sound.FromEntity( "nvl.voice.fire", user );
			}
				

			IsReloaded = false;

			ShootCannonball();
			
		}

		return false;
	}

	public async void ShootCannonball() 
	{

		WickSound = Sound.FromEntity( "nvl.blackpowdercannon.wick", this );
		//Particles.Create( "particles/naval_fuze_sparks.vpcf", this, "spark" ); --attachment does not work, I need to hardcode position
		Particles.Create( "particles/naval_fuze_sparks.vpcf", Transform.PointToWorld( new Vector3( 0, 33, 14 ) ) );

		await GameTask.DelayRealtimeSeconds( 1.2f );

		WickSound.Stop();

		Sound.FromEntity( "nvl.blackpowdercannon.fire", this );

		//Particles.Create( "particles/naval_gunpowder_smoke.vpcf", this, "muzzle" ); //suddenly stoped working.. cool
		//Particles.Create( "particles/pistol_muzzleflash.vpcf", this, "muzzle" ); // this also is not working

		var PowderSmoke = Particles.Create( "particles/naval_gunpowder_smoke.vpcf", Transform.PointToWorld( new Vector3( 0, -59, 0 ) ) ); // i have to hardcode this now
		PowderSmoke.SetForward( 0, Transform.NormalToWorld( new Vector3( 0, -1, 0 ) ) );

		var MuzzleFlash = Particles.Create( "particles/pistol_muzzleflash.vpcf", Transform.PointToWorld( new Vector3( 0, -59, 0 ) ) );
		MuzzleFlash.SetForward( 0, Transform.NormalToWorld( new Vector3( 0, -1, 0 ) ) );

		//new Sandbox.ScreenShake.Perlin( 0.5f, 2.0f, 0.5f );

		// Create the cannon ball entity

		//var ShootPos = this.GetAttachment( "muzzle" ).Position; // TO:DO  Oh my fuckin god, API has changed I have no idea how to Fix IT!
		//var ShootAngle = this.GetAttachment( "muzzle" ).Rotation; // TO:DO  -||-

		var ShootPos = Transform.PointToWorld( new Vector3( 0 , -59, 0 ) ); //I had to hardcode positions for now since I cant just use an attachment as reference.. 
		var ShootAngle = Transform.RotationToWorld( Rotation.From( new Angles( 180f, 0, 180f )  ) );
		var ProjScale = Scale;

		var ent = new NavalCannonBallProjectile
		{
			Position = ShootPos,
			Rotation = ShootAngle,
			Scale = ProjScale,
		};
		ent.SetModel( "models/naval/props/props/cball.vmdl" );
		//ent.Velocity += ent.Transform.NormalToWorld( new Vector3( ProjectileVelocity, 0, 0 ) ); // this was working when GetAttachment() was also working correctly
		ent.Velocity += ent.Transform.NormalToWorld( new Vector3( 0, ProjectileVelocity, 0 ) );

		ent.CannonParent = this;

		//recoil
		this.Velocity += this.Transform.NormalToWorld( new Vector3(0, RecoilForce, 0) );
		//screen shake
		if ( IsLocalPawn )
		{
			new Sandbox.ScreenShake.Perlin( 0.5f, 2.0f, 0.5f );
		}

		// Reload
		await GameTask.DelayRealtimeSeconds( 2f );

		Sound.FromEntity( "nvl.blackpowdercannon.reload", this );
		IsReloaded = true;

	}

	public void Remove()
	{
		PhysicsGroup?.Wake();
		Delete();
	}

	public void OnPostPhysicsStep( float dt )
	{
		if ( !this.IsValid() )
			return;

		var body = PhysicsBody;
		if ( !body.IsValid() )
			return;
	}
}
