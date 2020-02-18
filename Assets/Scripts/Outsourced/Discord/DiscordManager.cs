using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Discord;

public class DiscordManager : MonoBehaviour
{
    // Start is called before the first frame update
    public Discord.Discord discord;

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        discord = new Discord.Discord(605026177131872276, (System.UInt64)Discord.CreateFlags.NoRequireDiscord);
        var activityManager = discord.GetActivityManager();
        var activity = new Discord.Activity
        {
            State = "In development",
            Details = "Testing multiplayer."
        };
        activityManager.UpdateActivity(activity, (res) =>
        {
            if (res == Discord.Result.Ok)
            {
                Debug.Log("Discord state updated!");
            }
        });
    }

    // Update is called once per frame
    void Update()
    {
        discord.RunCallbacks();
    }

    void OnDestroy()
    {
        discord.Dispose();
    }
}
