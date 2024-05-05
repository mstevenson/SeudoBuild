﻿namespace SeudoCI.Agent;

using System;
using System.Net.NetworkInformation;

/// <summary>
/// A unique build agent identifier. Agent names are human readable.
/// </summary>
public static class AgentName
{
    /// <summary>
    /// Produces a random human-readable agent identifier, for debugging purposes.
    /// </summary>
    public static string GetRandomName(Random random = null)
    {
        var rand = random ?? new Random();
        return GetName(rand);
    }

    /// <summary>
    /// Produces a deterministic human-readable agent identifier.
    /// The name is based on the MAC addresses of the agent's network interfaces.
    /// </summary>
    public static string GetUniqueAgentName()
    {
        int hash = GetMacAddresses().GetHashCode();
        var rand = new Random(hash);
        return GetName(rand);
    }

    /// <summary>
    /// Generates an agent name based on the given random seed.
    /// </summary>
    private static string GetName(Random rand)
    {
        var adjective = Adjectives[rand.Next(0, Adjectives.Length)];
        //var modifier = modifiers[rand.Next(0, modifiers.Length)];
        var animal = Animals[rand.Next(0, Animals.Length)];
        string name = $"{adjective}-{animal}";
        return name.ToLower();
    }

    /// <summary>
    /// Returns a concatenated string of the MAC addresses of all network
    /// interfaces.
    /// </summary>
    private static string GetMacAddresses()
    {
        var macAddresses = string.Empty;
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            macAddresses += nic.GetPhysicalAddress().ToString();
        }
        return macAddresses;
    }

    private static readonly string[] Adjectives =
    {
        "abiding",
        "academic",
        "accepting",
        "adorable",
        "adventurous",
        "affable",
        "affectionate",
        "agreeable",
        "altruistic",
        "ambitious",
        "amiable",
        "amicable",
        "amusing",
        "articulate",
        "artistic",
        "assertive",
        "astounding",
        "astute",
        "attractive",
        "authentic",
        "blissful",
        "bodacious",
        "bold",
        "brave",
        "bright",
        "brilliant",
        "bubbly",
        "buff",
        "calm",
        "careful",
        "casual",
        "charismatic",
        "charming",
        "cheerful",
        "chummy",
        "classy",
        "clever",
        "colossal",
        "communicative",
        "compassionate",
        "comforting",
        "commendable",
        "confidant",
        "conscientious",
        "considerate",
        "conspicuous",
        "contemplative",
        "convenient",
        "copacetic",
        "cosy",
        "courageous",
        "courteous",
        "creative",
        "cunning",
        "curious",
        "cutting-edge",
        "dainty",
        "dapper",
        "daring",
        "dazzling",
        "decisive",
        "dedicated",
        "delightful",
        "dependable",
        "determined",
        "devoted",
        "dexterous",
        "diligent",
        "diplomatic",
        "disarming",
        "discreet",
        "dynamic",
        "easygoing",
        "ecstatic",
        "educated",
        "elated",
        "elegant",
        "elemental",
        "eloquent",
        "electric",
        "emotional",
        "empathetic",
        "energetic",
        "enormous",
        "enthusiastic",
        "epicurean",
        "erudite",
        "esteemed",
        "exotic",
        "exuberant",
        "exquisite",
        "fabulous",
        "fair-minded",
        "faithful",
        "fancy",
        "fashionable",
        "fearless",
        "first-class",
        "forceful",
        "frank",
        "friendly",
        "generous",
        "genial",
        "gentle",
        "giddy",
        "glamorous",
        "good",
        "grateful",
        "gregarious",
        "groovy",
        "handsome",
        "hard-working",
        "harmless",
        "helpful",
        "high-class",
        "honest",
        "hopeful",
        "hospitable",
        "humble",
        "humorous",
        "hypnotic",
        "idealistic",
        "imaginative",
        "impartial",
        "impeccable",
        "independent",
        "industrious",
        "infallible",
        "intellectual",
        "intelligent",
        "intuitive",
        "inventive",
        "inquisitive",
        "inspiring",
        "intrepid",
        "irresistible",
        "jaunty",
        "jolly",
        "jubilant",
        "keen",
        "kind",
        "literary",
        "logical",
        "loving",
        "loyal",
        "lucid",
        "magical",
        "marvelous",
        "mellow",
        "methodical",
        "modest",
        "motivated",
        "mysterious",
        "neat",
        "nimble",
        "noble",
        "observant",
        "optimistic",
        "orderly",
        "outstanding",
        "passionate",
        "patient",
        "persistent",
        "pioneering",
        "philosophical",
        "placid",
        "plucky",
        "polite",
        "powerful",
        "practical",
        "pragmatic",
        "precise",
        "proficient",
        "prolific",
        "proud",
        "quiet",
        "quirky",
        "rad",
        "refined",
        "rational",
        "reliable",
        "reserved",
        "resourceful",
        "romantic",
        "sassy",
        "savvy",
        "self-confident",
        "self-disciplined",
        "sensible",
        "sensitive",
        "shy",
        "sincere",
        "sociable",
        "straightforward",
        "superb",
        "sympathetic",
        "systematic",
        "tactful",
        "tasteful",
        "thrilling",
        "thoughtful",
        "tidy",
        "tough",
        "tranquil",
        "unassuming",
        "uncommon",
        "understanding",
        "unobtrusive",
        "venerable",
        "versatile",
        "vibrant",
        "virtuous",
        "warmhearted",
        "well-behaved",
        "well-informed",
        "well-known",
        "well-spoken",
        "whimsical",
        "witty"
    };

    private static readonly string[] Modifiers =
    {
        "asian",
        "australian",
        "african",
        "european",
        "prehistoric",
        "blue",
        "red",
        "brown",
        "fuzzy",
        "amphibious",
        "aquatic",
        "common",
        "domestic",
        "fluffy",
        "freshwater",
        "giant",
        "horned",
        "juvenile",
        "migratory",
        "native",
        "nocturnal",
        "omnivorous",
        "carnivorous",
        "pigmy",
        "saltwater",
        "webbed",
        "winged",
        "spiny",
        "miniature",
        "sea",
        "mountain",
        "wild",
        "canadian",
        "northern",
        "southern",
        "western",
        "eastern",
        "hibernating"
    };

    // Based on https://github.com/hzlzh/Domain-Name-List/blob/master/Animal-words.txt
    private static readonly string[] Animals =
    {
        "Aardvark",
        "Albatross",
        "Alligator",
        "Alpaca",
        "Anteater",
        "Antelope",
        "Armadillo",
        "Badger",
        "Barracuda",
        "Bat",
        "Bear",
        "Beaver",
        "Bee",
        "Bison",
        "Buffalo",
        "Butterfly",
        "Camel",
        "Caribou",
        "Cat",
        "Caterpillar",
        "Cattle",
        "Cheetah",
        "Chicken",
        "Chinchilla",
        "Clam",
        "Cobra",
        "Cockroach",
        "Cod",
        "Cormorant",
        "Coyote",
        "Crab",
        "Crane",
        "Crocodile",
        "Crow",
        "Curlew",
        "Deer",
        "Dinosaur",
        "Dolphin",
        "Dotterel",
        "Dove",
        "Dragonfly",
        "Duck",
        "Dugong",
        "Dunlin",
        "Eagle",
        "Echidna",
        "Eel",
        "Elephant",
        "Elk",
        "Emu",
        "Ferret",
        "Finch",
        "Fish",
        "Flamingo",
        "Fly",
        "Fox",
        "Frog",
        "Gazelle",
        "Gerbil",
        "Giraffe",
        "Goat",
        "Goose",
        "Goldfish",
        "Gopher",
        "Gorilla",
        "Grasshopper",
        "Guinea-pig",
        "Hamster",
        "Hedgehog",
        "Heron",
        "Herring",
        "Hippo",
        "Hornet",
        "Horse",
        "Hummingbird",
        "Jaguar",
        "Jay",
        "Jellyfish",
        "Kakapo",
        "Kangaroo",
        "Koala",
        "Lark",
        "Lemur",
        "Lemming",
        "Leopard",
        "Lion",
        "Lizard",
        "Llama",
        "Lobster",
        "Loris",
        "Louse",
        "Lyrebird",
        "Magpie",
        "Mallard",
        "Manatee",
        "Marten",
        "Meerkat",
        "Mink",
        "Mole",
        "Monkey",
        "Moose",
        "Mouse",
        "Mosquito",
        "Newt",
        "Octopus",
        "Opossum",
        "Oryx",
        "Ostrich",
        "Otter",
        "Owl",
        "Ox",
        "Oyster",
        "Panther",
        "Parrot",
        "Partridge",
        "Pelican",
        "Penguin",
        "Pheasant",
        "Pigeon",
        "Pony",
        "Porcupine",
        "Porpoise",
        "Puffin",
        "Quail",
        "Rabbit",
        "Raccoon",
        "Ram",
        "Raven",
        "Reindeer",
        "Rhino",
        "Salamander",
        "Salmon",
        "Sandpiper",
        "Sardine",
        "Scorpion",
        "Seahorse",
        "Seal",
        "Shark",
        "Sheep",
        "Shrew",
        "Shrimp",
        "Skunk",
        "Snail",
        "Snake",
        "Spider",
        "Squid",
        "Squirrel",
        "Starling",
        "Stingray",
        "Stork",
        "Swallow",
        "Swan",
        "Tapir",
        "Tiger",
        "Toad",
        "Trout",
        "Turkey",
        "Turtle",
        "Wallaby",
        "Walrus",
        "Weasel",
        "Whale",
        "Wolf",
        "Wolverine",
        "Wombat",
        "Woodcock",
        "Woodpecker",
        "Worm",
        "Yak",
        "Zebra"
    };
}