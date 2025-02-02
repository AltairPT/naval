﻿namespace Sandbox.Tools
{
	[Library( "tool_physweld", Title = "Weld (physical)", Description = "Weld stuff together", Group = "construction" )]
	public partial class PhysWeldTool : BaseTool
	{
		private Prop target;

		public override void Simulate()
		{
			if ( !Host.IsServer )
				return;

			using ( Prediction.Off() )
			{
				var startPos = Owner.EyePos;
				var dir = Owner.EyeRot.Forward;

				var tr = Trace.Ray( startPos, startPos + dir * MaxTraceDistance )
					.Ignore( Owner )
					.Run();

				if ( !tr.Hit || !tr.Body.IsValid() || !tr.Entity.IsValid() || tr.Entity.IsWorld )
					return;

				if ( tr.Entity.PhysicsGroup == null || tr.Entity.PhysicsGroup.BodyCount > 1 )
					return;

				if ( tr.Entity is not Prop prop )
					return;

				if ( Input.Pressed( InputButton.Attack1 ) )
				{
					if ( prop.Root is not Prop rootProp )
					{
						return;
					}

					if ( target == rootProp )
						return;

					if ( !target.IsValid() )
					{
						target = rootProp;
					}
					else
					{
						//target.Weld( rootProp ); //this is actually a parent, not traditional weld 
						// Lets use physical constraint, buoyeancy works much better with this
						var ConstraintWeld = PhysicsJoint.Weld
							.From( tr.Body )
							.To( target.PhysicsBody, tr.Body.Transform.PointToLocal( tr.Body.Position ), target.PhysicsBody.Transform.RotationToLocal( tr.Body.Rotation ) )
							.WithCollisionsEnabled()
							.Create();

						target = null;
					}
				}
				else if ( Input.Pressed( InputButton.Attack2 ) )
				{
					prop.Unweld( true );

					Reset();
				}
				else if ( Input.Pressed( InputButton.Reload ) )
				{
					if ( prop.Root is not Prop rootProp )
					{
						return;
					}

					rootProp.Unweld();

					Reset();
				}
				else
				{
					return;
				}

				CreateHitEffects( tr.EndPos );
			}
		}

		private void Reset()
		{
			target = null;
		}

		public override void Activate()
		{
			base.Activate();

			Reset();
		}

		public override void Deactivate()
		{
			base.Deactivate();

			Reset();
		}
	}
}
