using System;

public class NetworkedProjectileRPCEventArgs : EventArgs
{
	public string Type { get; private set; }

	public NetworkedProjectileRPCEventArgs(string type)
	{
		Type = type;
	}
}
