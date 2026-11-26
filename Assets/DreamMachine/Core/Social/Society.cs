using System;
using System.Collections.Generic;
using DreamMachine.Util;

namespace DreamMachine.Social
{
    public enum Sex
    {
        Female = 0,
        Male = 1
    }

    public enum GroupId
    {
        Boys = 0,
        Girls = 1
    }

    /// <summary>
    /// A configurable social pattern, not a claim about real families.
    /// Patriarchal here means a male leader sets the group's goal.
    /// Matriarchal means a female leader sets it, and the other mentors follow her.
    /// </summary>
    public enum GroupStyle
    {
        Patriarchal = 0,
        Matriarchal = 1,
        Household = 2
    }

    public enum FamilyRoutine
    {
        MorningMeal = 0,
        Play = 1,
        EveningMeal = 2,
        Bedtime = 3
    }

    public enum MemberRole
    {
        Leader = 0,
        Mentor = 1,
        Baby = 2
    }

    public enum GroupGoal
    {
        WalkTogether = 0,
        PracticeGesture = 1,
        Rest = 2,
        InspectObjects = 3
    }

    public sealed class PersonSpec
    {
        public string Id;
        public string DisplayName;
        public Sex Sex;
        public GroupId Group;
        public GroupStyle Style;
        public MemberRole Role;
        public bool IsLeader;
        public string PairedWithId;
        public int BirthSeed;
    }

    public sealed class PopulationConfig
    {
        public int BabyCount;
        public int MentorCount;
        public bool IsHousehold;
        public List<PersonSpec> People = new List<PersonSpec>();

        /// <summary>
        /// The default cast is a household of four. Display names are placeholders.
        /// Rename them in family.json; ids stay father, mother, son, daughter.
        /// </summary>
        public static PopulationConfig CreateDefault()
        {
            return CreateHousehold(null);
        }

        public static PopulationConfig CreateHousehold(Dictionary<string, string> names)
        {
            string father = Name(names, "father", "Father");
            string mother = Name(names, "mother", "Mother");
            string son = Name(names, "son", "Son");
            string daughter = Name(names, "daughter", "Daughter");
            var pop = new PopulationConfig();
            pop.IsHousehold = true;
            var dad = Member("father", father, Sex.Male, GroupId.Boys, MemberRole.Leader, true, 101);
            var boy = Member("son", son, Sex.Male, GroupId.Boys, MemberRole.Baby, false, 303);
            var mom = Member("mother", mother, Sex.Female, GroupId.Girls, MemberRole.Leader, true, 202);
            var girl = Member("daughter", daughter, Sex.Female, GroupId.Girls, MemberRole.Baby, false, 404);
            dad.Style = GroupStyle.Household;
            boy.Style = GroupStyle.Household;
            mom.Style = GroupStyle.Household;
            girl.Style = GroupStyle.Household;
            dad.PairedWithId = boy.Id;
            boy.PairedWithId = dad.Id;
            mom.PairedWithId = girl.Id;
            girl.PairedWithId = mom.Id;
            pop.People.Add(dad);
            pop.People.Add(mom);
            pop.People.Add(boy);
            pop.People.Add(girl);
            pop.Validate();
            return pop;
        }

        public static PopulationConfig FromJson(string json)
        {
            var root = MiniJson.AsObject(MiniJson.Parse(json));
            var names = new Dictionary<string, string>();
            if (root.ContainsKey("members"))
            {
                List<object> rows = MiniJson.AsArray(root["members"]);
                for (int i = 0; i < rows.Count; i++)
                {
                    var row = MiniJson.AsObject(rows[i]);
                    string id = MiniJson.AsString(row["id"]);
                    string name = row.ContainsKey("name") ? MiniJson.AsString(row["name"]) : id;
                    if (id.Length > 0) names[id] = name;
                }
            }
            return CreateHousehold(names);
        }

        static string Name(Dictionary<string, string> names, string id, string fallback)
        {
            string value;
            if (names != null && names.TryGetValue(id, out value) && value != null && value.Length > 0) return value;
            return fallback;
        }

        static PersonSpec Member(string id, string display, Sex sex, GroupId group, MemberRole role, bool leader, int seed)
        {
            return new PersonSpec
            {
                Id = id,
                DisplayName = display,
                Sex = sex,
                Group = group,
                Role = role,
                IsLeader = leader,
                BirthSeed = seed
            };
        }

        public PersonSpec PrimaryRoleModel(PersonSpec child)
        {
            if (child == null) return null;
            if (IsHousehold || child.Id == "son" || child.Id == "daughter")
            {
                string id = child.Sex == Sex.Male ? "father" : "mother";
                PersonSpec parent = Find(id);
                if (parent != null) return parent;
            }
            PersonSpec paired = Find(child.PairedWithId);
            if (paired != null && paired.Role != MemberRole.Baby) return paired;
            return LeaderOf(child.Group);
        }

        public PersonSpec OtherParent(PersonSpec child)
        {
            if (child == null) return null;
            string id = child.Sex == Sex.Male ? "mother" : "father";
            return Find(id);
        }

        public PersonSpec SiblingOf(PersonSpec child)
        {
            if (child == null) return null;
            string id = child.Id == "son" ? "daughter" : child.Id == "daughter" ? "son" : null;
            return id == null ? null : Find(id);
        }

        public static FamilyRoutine RoutineAt(float daySeconds, float dayLength, float awakeLength)
        {
            float t = daySeconds % dayLength;
            if (t < 0f) t += dayLength;
            if (t >= awakeLength) return FamilyRoutine.Bedtime;
            float u = awakeLength <= 0f ? 0f : t / awakeLength;
            if (u < 0.18f) return FamilyRoutine.MorningMeal;
            if (u < 0.72f) return FamilyRoutine.Play;
            return FamilyRoutine.EveningMeal;
        }

        /// <summary>
        /// perGroup babies in each group, each with one mentor.
        /// Boys' group: one male leader and the other mentors female.
        /// Girls' group: one female leader and the other mentors male.
        /// </summary>
        public static PopulationConfig CreateBalanced(int perGroup)
        {
            if (perGroup < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(perGroup), "Each group needs at least one pair.");
            }
            var pop = new PopulationConfig();
            string[] boyBabies = { "Milo", "Jonah", "Theo", "Nico", "Owen", "Felix" };
            string[] girlBabies = { "Ada", "Lila", "Nora", "Iris", "Esme", "Willa" };
            string[] femaleMentors = { "Maren", "Sable", "Nell", "Priya", "Hana" };
            string[] maleMentors = { "Calder", "Ivo", "Jonas", "Rafa", "Leo" };
            AddGroup(pop, GroupId.Boys, GroupStyle.Patriarchal, Sex.Male, "Harun",
                femaleMentors, boyBabies, perGroup, 100);
            AddGroup(pop, GroupId.Girls, GroupStyle.Matriarchal, Sex.Female, "Vesper",
                maleMentors, girlBabies, perGroup, 200);
            pop.Validate();
            return pop;
        }

        static void AddGroup(
            PopulationConfig pop,
            GroupId group,
            GroupStyle style,
            Sex leaderSex,
            string leaderName,
            string[] otherMentorNames,
            string[] babyNames,
            int perGroup,
            int seedBase)
        {
            var leader = new PersonSpec
            {
                Id = Slug(leaderName),
                DisplayName = leaderName,
                Sex = leaderSex,
                Group = group,
                Style = style,
                Role = MemberRole.Leader,
                IsLeader = true,
                BirthSeed = seedBase + 1
            };
            pop.People.Add(leader);
            var mentors = new List<PersonSpec> { leader };
            for (int i = 0; i < perGroup - 1; i++)
            {
                string name = otherMentorNames[i % otherMentorNames.Length];
                if (perGroup > otherMentorNames.Length + 1)
                {
                    name = name + (i + 1);
                }
                var mentor = new PersonSpec
                {
                    Id = Slug(name),
                    DisplayName = name,
                    Sex = leaderSex == Sex.Male ? Sex.Female : Sex.Male,
                    Group = group,
                    Style = style,
                    Role = MemberRole.Mentor,
                    IsLeader = false,
                    BirthSeed = seedBase + 2 + i
                };
                mentors.Add(mentor);
                pop.People.Add(mentor);
            }
            for (int i = 0; i < perGroup; i++)
            {
                string name = babyNames[i % babyNames.Length];
                if (i >= babyNames.Length) name = name + (i + 1);
                var baby = new PersonSpec
                {
                    Id = Slug(name),
                    DisplayName = name,
                    Sex = group == GroupId.Boys ? Sex.Male : Sex.Female,
                    Group = group,
                    Style = style,
                    Role = MemberRole.Baby,
                    IsLeader = false,
                    PairedWithId = mentors[i].Id,
                    BirthSeed = seedBase + 40 + i
                };
                mentors[i].PairedWithId = baby.Id;
                pop.People.Add(baby);
            }
        }

        public void Validate()
        {
            int babies = 0;
            int mentors = 0;
            var pairOwners = new HashSet<string>();
            var groups = new Dictionary<GroupId, List<PersonSpec>>();
            for (int i = 0; i < People.Count; i++)
            {
                PersonSpec person = People[i];
                if (person.Role == MemberRole.Baby) babies++;
                else mentors++;
                if (!groups.ContainsKey(person.Group)) groups[person.Group] = new List<PersonSpec>();
                groups[person.Group].Add(person);
            }
            BabyCount = babies;
            MentorCount = mentors;
            if (babies != mentors)
            {
                throw new InvalidOperationException(
                    "Babies and mentors must be paired one-to-one. Counts differ: "
                    + babies + " babies, " + mentors + " mentors.");
            }
            if (babies == 0)
            {
                throw new InvalidOperationException("Population needs at least one baby-mentor pair.");
            }
            for (int i = 0; i < People.Count; i++)
            {
                PersonSpec person = People[i];
                if (string.IsNullOrEmpty(person.PairedWithId))
                {
                    throw new InvalidOperationException(person.DisplayName + " has no pair.");
                }
                PersonSpec partner = Find(person.PairedWithId);
                if (partner == null || partner.PairedWithId != person.Id)
                {
                    throw new InvalidOperationException(person.DisplayName + " is not in a mutual pair.");
                }
                if (person.Group != partner.Group)
                {
                    throw new InvalidOperationException(person.DisplayName + " is paired outside their group.");
                }
                if (person.Role == MemberRole.Baby && partner.Role == MemberRole.Baby)
                {
                    throw new InvalidOperationException(person.DisplayName + " is paired with another baby.");
                }
                if (!pairOwners.Add(CanonicalPair(person.Id, partner.Id)))
                {
                    // The partner adds the same key. That is expected once.
                }
            }
            if (pairOwners.Count != babies)
            {
                throw new InvalidOperationException("Pairing is not one-to-one.");
            }
            foreach (var pair in groups)
            {
                int leaders = 0;
                PersonSpec leader = null;
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    if (pair.Value[i].IsLeader)
                    {
                        leaders++;
                        leader = pair.Value[i];
                    }
                }
                if (leaders != 1 || leader == null)
                {
                    throw new InvalidOperationException("Group " + pair.Key + " needs exactly one leader.");
                }
                if (leader.Style == GroupStyle.Patriarchal && leader.Sex != Sex.Male)
                {
                    throw new InvalidOperationException("A patriarchal group leader must be male.");
                }
                if (leader.Style == GroupStyle.Matriarchal && leader.Sex != Sex.Female)
                {
                    throw new InvalidOperationException("A matriarchal group leader must be female.");
                }
                if (leader.Style == GroupStyle.Household && leader.Role == MemberRole.Baby)
                {
                    throw new InvalidOperationException("A household parent leads the group.");
                }
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    PersonSpec member = pair.Value[i];
                    if (member.Role == MemberRole.Baby)
                    {
                        Sex expected = pair.Key == GroupId.Boys ? Sex.Male : Sex.Female;
                        if (member.Sex != expected)
                        {
                            throw new InvalidOperationException(member.DisplayName + " is in the wrong sex group.");
                        }
                    }
                    else if (!member.IsLeader)
                    {
                        Sex expected = leader.Sex == Sex.Male ? Sex.Female : Sex.Male;
                        if (member.Sex != expected)
                        {
                            throw new InvalidOperationException(
                                member.DisplayName + " should be the other sex from the leader they follow.");
                        }
                    }
                }
            }
        }

        public PersonSpec Find(string id)
        {
            for (int i = 0; i < People.Count; i++)
            {
                if (People[i].Id == id) return People[i];
            }
            return null;
        }

        public PersonSpec LeaderOf(GroupId group)
        {
            for (int i = 0; i < People.Count; i++)
            {
                if (People[i].Group == group && People[i].IsLeader) return People[i];
            }
            return null;
        }

        public List<PersonSpec> Babies()
        {
            var list = new List<PersonSpec>();
            for (int i = 0; i < People.Count; i++)
            {
                if (People[i].Role == MemberRole.Baby) list.Add(People[i]);
            }
            return list;
        }

        public List<PersonSpec> Mentors()
        {
            var list = new List<PersonSpec>();
            for (int i = 0; i < People.Count; i++)
            {
                if (People[i].Role != MemberRole.Baby) list.Add(People[i]);
            }
            return list;
        }

        static string CanonicalPair(string a, string b)
        {
            return string.CompareOrdinal(a, b) < 0 ? a + "|" + b : b + "|" + a;
        }

        static string Slug(string name)
        {
            return name.ToLowerInvariant().Replace(" ", "-");
        }
    }

    public static class SocialAttention
    {
        /// <summary>
        /// Who a baby looks at. The group leader is the identity reference
        /// and the highest mimicry weight. The paired mentor is next.
        /// Other babies and mentors are still visible, so nobody is alone.
        /// </summary>
        public static float AttentionWeight(PersonSpec baby, PersonSpec other)
        {
            if (baby == null || other == null || baby.Id == other.Id) return 0f;
            if (IsHouseholdMember(baby.Id) && IsHouseholdMember(other.Id))
            {
                bool sameGenderParent = other.IsLeader && other.Sex == baby.Sex;
                bool otherParent = other.IsLeader && other.Sex != baby.Sex;
                if (sameGenderParent) return 1.00f;
                if (otherParent) return 0.74f;
                if (other.Role == MemberRole.Baby) return 0.48f;
                return 0.2f;
            }
            if (other.IsLeader && other.Group == baby.Group) return 1.00f;
            if (other.Id == baby.PairedWithId) return 0.72f;
            if (other.Role != MemberRole.Baby && other.Group == baby.Group) return 0.46f;
            if (other.Role == MemberRole.Baby && other.Group == baby.Group) return 0.40f;
            if (other.IsLeader) return 0.36f;
            if (other.Role == MemberRole.Baby) return 0.28f;
            return 0.18f;
        }

        public static bool IsHouseholdMember(string id)
        {
            return id == "father" || id == "mother" || id == "son" || id == "daughter";
        }

        public static float BondParentChild(PersonSpec child, PersonSpec parent)
        {
            if (child == null || parent == null || !parent.IsLeader) return 0f;
            return parent.Sex == child.Sex ? 1f : 0.72f;
        }

        public static float BondSibling()
        {
            return 0.55f;
        }

        public static float BondCouple()
        {
            return 0.9f;
        }

        public static float RoleModelWeight(PersonSpec baby, PersonSpec other)
        {
            return AttentionWeight(baby, other);
        }

        /// <summary>
        /// Mentors who are not the leader take the leader's goal.
        /// In the girls' group that leader is a woman. In the boys' group, a man.
        /// </summary>
        public static float DutyToLeader(PersonSpec person)
        {
            if (person == null) return 0f;
            if (person.IsLeader) return 1f;
            if (person.Role == MemberRole.Baby) return 0.55f;
            return 0.85f;
        }
    }

    public sealed class GroupMind
    {
        public GroupGoal BoysGoal;
        public GroupGoal GirlsGoal;

        public void Step(int day, int tick, int birthSalt)
        {
            var rng = new SeededRandom(birthSalt + day * 17 + tick);
            BoysGoal = (GroupGoal)rng.NextInt(4);
            GirlsGoal = (GroupGoal)rng.NextInt(4);
        }

        public GroupGoal GoalFor(PersonSpec person)
        {
            if (person == null) return GroupGoal.WalkTogether;
            return person.Group == GroupId.Boys ? BoysGoal : GirlsGoal;
        }
    }

    public sealed class AppearanceProfile
    {
        public string Id;
        public string DisplayName;
        public string Sex;
        public bool IsLeader;
        public bool IsBaby;
        public string Skin;
        public string Hair;
        public string Cloth;
        public string Accent;
        public string HairStyle;
        public string Outfit;
        public string FacialHair;
        public string Marker;
        public float Height;
        public float Shoulder;
        public float Hip;
        public float Head;
    }

    public static class AppearanceCatalog
    {
        public static List<AppearanceProfile> Defaults(PopulationConfig pop)
        {
            var list = new List<AppearanceProfile>();
            for (int i = 0; i < pop.People.Count; i++)
            {
                list.Add(For(pop.People[i]));
            }
            return list;
        }

        public static AppearanceProfile For(PersonSpec person)
        {
            bool female = person.Sex == Sex.Female;
            float height = person.Role == MemberRole.Baby ? 0.72f : (female ? 0.98f : 1.02f);
            float shoulder = female ? 0.92f : 1.08f;
            float hip = female ? 1.12f : 0.94f;
            if (person.IsLeader) height += 0.14f;
            var look = Palette(person.Id);
            return new AppearanceProfile
            {
                Id = person.Id,
                DisplayName = person.DisplayName,
                Sex = person.Sex.ToString(),
                IsLeader = person.IsLeader,
                IsBaby = person.Role == MemberRole.Baby,
                Skin = look.Skin,
                Hair = look.Hair,
                Cloth = look.Cloth,
                Accent = person.IsLeader ? "#F2C14E" : look.Accent,
                HairStyle = person.IsLeader ? "crown" : look.HairStyle,
                Outfit = person.IsLeader ? "robe" : look.Outfit,
                FacialHair = look.FacialHair,
                Marker = person.IsLeader ? "circlet" : "none",
                Height = height,
                Shoulder = shoulder,
                Hip = hip,
                Head = person.Role == MemberRole.Baby ? 1.12f : 1f
            };
        }

        struct PaletteRow
        {
            public string Skin, Hair, Cloth, Accent, HairStyle, Outfit, FacialHair;
        }

        static PaletteRow Palette(string id)
        {
            switch (id)
            {
                case "father": return Row("#6B3E2E", "#1A1A1A", "#F4F1EA", "#C4A46A", "crop", "robe", "short-beard");
                case "mother": return Row("#E0B090", "#3A241C", "#8E1E3F", "#F2C14E", "long", "robe", "none");
                case "son": return Row("#E2B48C", "#7A4B32", "#3D6F9A", "#F0D8B0", "crop", "tunic", "none");
                case "daughter": return Row("#F3D3BE", "#2A211C", "#C45B78", "#F6D7C3", "bob", "wrap", "none");
                case "harun": return Row("#6B3E2E", "#1A1A1A", "#F4F1EA", "#C4A46A", "crop", "robe", "short-beard");
                case "maren": return Row("#C48A6A", "#6E4B3A", "#2C3E73", "#D9C7A3", "long", "wrap", "none");
                case "sable": return Row("#4A2C24", "#1B1B1B", "#B8613A", "#E7D3B0", "crop", "tunic", "none");
                case "vesper": return Row("#E0B090", "#3A241C", "#8E1E3F", "#F2C14E", "long", "robe", "none");
                case "calder": return Row("#E6C2A2", "#5C3A28", "#4E5964", "#C9B79C", "swept", "tunic", "short-beard");
                case "ivo": return Row("#C4895A", "#111111", "#2F5D50", "#D7C4A3", "crop", "vest", "none");
                case "milo": return Row("#E2B48C", "#7A4B32", "#3D6F9A", "#F0D8B0", "crop", "tunic", "none");
                case "jonah": return Row("#F0D2B4", "#2C2118", "#3E7A55", "#E7C8A2", "swept", "vest", "none");
                case "theo": return Row("#6A4332", "#141414", "#C47B2B", "#E6C39A", "crop", "tunic", "none");
                case "ada": return Row("#F3D3BE", "#2A211C", "#C45B78", "#F6D7C3", "bob", "wrap", "none");
                case "lila": return Row("#C98A62", "#1A120E", "#2A8C8C", "#E8CDB4", "long", "wrap", "none");
                case "nora": return Row("#F6D7C3", "#8C3B22", "#E1A322", "#F8E2C0", "bun", "wrap", "none");
                default: return Row("#E2B48C", "#3A2A22", "#6E8B74", "#E6D2B5", "crop", "tunic", "none");
            }
        }

        static PaletteRow Row(string skin, string hair, string cloth, string accent, string hairStyle, string outfit, string facial)
        {
            return new PaletteRow
            {
                Skin = skin,
                Hair = hair,
                Cloth = cloth,
                Accent = accent,
                HairStyle = hairStyle,
                Outfit = outfit,
                FacialHair = facial
            };
        }

        public static string ToJson(List<AppearanceProfile> profiles)
        {
            var rows = new List<object>();
            for (int i = 0; i < profiles.Count; i++)
            {
                AppearanceProfile p = profiles[i];
                rows.Add(new Dictionary<string, object>
                {
                    { "id", p.Id },
                    { "displayName", p.DisplayName },
                    { "sex", p.Sex },
                    { "isLeader", p.IsLeader },
                    { "isBaby", p.IsBaby },
                    { "skin", p.Skin },
                    { "hair", p.Hair },
                    { "cloth", p.Cloth },
                    { "accent", p.Accent },
                    { "hairStyle", p.HairStyle },
                    { "outfit", p.Outfit },
                    { "facialHair", p.FacialHair },
                    { "marker", p.Marker },
                    { "height", p.Height },
                    { "shoulder", p.Shoulder },
                    { "hip", p.Hip },
                    { "head", p.Head }
                });
            }
            return MiniJson.Stringify(new Dictionary<string, object> { { "looks", rows } });
        }

        public static List<AppearanceProfile> FromJson(string json)
        {
            var root = MiniJson.AsObject(MiniJson.Parse(json));
            var rows = MiniJson.AsArray(root["looks"]);
            var list = new List<AppearanceProfile>();
            for (int i = 0; i < rows.Count; i++)
            {
                var row = MiniJson.AsObject(rows[i]);
                list.Add(new AppearanceProfile
                {
                    Id = MiniJson.AsString(row["id"]),
                    DisplayName = MiniJson.AsString(row["displayName"]),
                    Sex = MiniJson.AsString(row["sex"]),
                    IsLeader = MiniJson.AsBool(row["isLeader"]),
                    IsBaby = MiniJson.AsBool(row["isBaby"]),
                    Skin = MiniJson.AsString(row["skin"]),
                    Hair = MiniJson.AsString(row["hair"]),
                    Cloth = MiniJson.AsString(row["cloth"]),
                    Accent = MiniJson.AsString(row["accent"]),
                    HairStyle = MiniJson.AsString(row["hairStyle"]),
                    Outfit = MiniJson.AsString(row["outfit"]),
                    FacialHair = MiniJson.AsString(row["facialHair"]),
                    Marker = MiniJson.AsString(row["marker"]),
                    Height = MiniJson.AsFloat(row["height"]),
                    Shoulder = MiniJson.AsFloat(row["shoulder"]),
                    Hip = MiniJson.AsFloat(row["hip"]),
                    Head = MiniJson.AsFloat(row["head"])
                });
            }
            return list;
        }
    }
}
