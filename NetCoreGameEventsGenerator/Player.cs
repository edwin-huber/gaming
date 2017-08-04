namespace NetCoreGameEventsGenerator
{
    class Player
    {
        public string Name { get; set; }

        public Location PlayerLocation { get; set; }
        public Player(string name, Location playerLocation)
        {
            Name = name;
            PlayerLocation = playerLocation;
        }
    }
}
