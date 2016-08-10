using System;

public interface IProjectile
{
	event EventHandler<NetworkedProjectileRPCEventArgs> FireRPC;

	void HandleRPC(string type);

	void Init(bool isMine);
}
