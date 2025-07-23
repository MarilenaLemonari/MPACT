using System;
using UnityEngine;

public class BehaviorProfile
{
    public float goal;
    public float group;
    public float interaction;
    public float connection;

    public enum Type
    {
        Goal,
        Group,
        Interact,
        Connectivity        
    }
    
    public BehaviorProfile()
    {
        goal = 0f;
        group = 0f;
        interaction = 0f;
        connection = 0.5f;
    }
    
    public BehaviorProfile(BehaviorProfile other)
    {
        goal = other.goal;
        group = other.group;
        interaction = other.interaction;
        connection = other.connection;
    }
        
    public BehaviorProfile(float go, float gr, float inter, float conn)
    {
        goal = go;
        group = gr;
        interaction = inter;
        connection = conn;
    }
    
    public bool IsSimilar(BehaviorProfile other, float threshold)
    {
        return Math.Abs(this.goal - other.goal) <= threshold
               && Math.Abs(this.group - other.group) <= threshold
               && Math.Abs(this.interaction - other.interaction) <= threshold
               && Math.Abs(this.connection - other.connection) <= threshold;
    }
    
    public BehaviorProfile LerpProfile(BehaviorProfile other, float t)
    {
        BehaviorProfile result = new BehaviorProfile();

        result.goal = Mathf.Lerp(this.goal, other.goal, t);
        result.group = Mathf.Lerp(this.group, other.group, t);
        result.interaction = Mathf.Lerp(this.interaction, other.interaction, t);
        result.connection = Mathf.Lerp(this.connection, other.connection, t);

        return result;
    }
    
    public BehaviorProfile Multiply(float weight)
    {
        return new BehaviorProfile
        {
            goal = this.goal * weight,
            group = this.group * weight,
            interaction = this.interaction * weight,
            connection = this.connection * weight
        };
    }
    
    public BehaviorProfile Add(BehaviorProfile other)
    {
        return new BehaviorProfile
        {
            goal = this.goal + other.goal,
            group = this.group + other.group,
            interaction = this.interaction + other.interaction,
            connection = this.connection + other.connection
        };
    }

}


