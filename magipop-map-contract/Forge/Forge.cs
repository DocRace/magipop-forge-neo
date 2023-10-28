using System;
using System.ComponentModel;
using System.Numerics;

using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using Neo.SmartContract;

namespace ForgeToken
{
    [DisplayName("magipop.ForgeTokenContract")]
    [ManifestExtra("Author", "Blink Chen")]
    [ManifestExtra("Email", "blink@magipop.xyz")]
    [ManifestExtra("Description", "magipop ForgeToken")]
    [SupportedStandards("NEP-17")]
    [ContractPermission("*", "onNEP17Payment")]
    public class ForgeTokenContract : Nep17Token
    {
        // public StorageMap _owners = new StorageMap(Storage.CurrentContext, "owners".ToByteArray());
        public static Map<UInt160, bool> _owners = new Map<UInt160, bool>();
        public static int _transactionIdCounter = 0;
        public static int _numConfirmationsRequired = 1;

        public struct MagipopTransaction {
            public UInt160 to;
            public BigInteger amount;
            public int confirmations;
            public bool executed;
            public bool denied;
        }

        [InitialValue("NcRtxmtaNPTpFJQRuuWafFKRVgLfqbL2ub", ContractParameterType.Hash160)]
        private static readonly UInt160 owner = default;
        // Prefix_TotalSupply = 0x00; Prefix_Balance = 0x01;
        private const byte Prefix_Contract = 0x02;
        public static readonly StorageMap ContractMap = new StorageMap(Storage.CurrentContext, Prefix_Contract);
        private static readonly byte[] ownerKey = "owner".ToByteArray();
        private static readonly byte[] adminKey = "admin".ToByteArray();
        private static bool IsOwner() => Runtime.CheckWitness(GetOwner());
        public override byte Decimals() => Factor();
        public override string Symbol() => "BBLE";

        public static byte Factor() => 8;

        public static void _deploy(object data, bool update)
        {
            if (update) return;
            ContractMap.Put(ownerKey, owner);
            ContractMap.Put(adminKey, owner);
            Nep17Token.Mint(owner, 21000000 * BigInteger.Pow(10, Factor()));
        }

        public static UInt160 GetOwner()
        {
            return (UInt160)ContractMap.Get(ownerKey);
        }

        public static UInt160 GetAdmin()
        {
            return (UInt160)ContractMap.Get(adminKey);
        }

        public static new void Mint(UInt160 account, BigInteger amount)
        {
            if (!IsOwner()) throw new InvalidOperationException("No Authorization!");
            Nep17Token.Mint(account, amount);
        }

        public static new void Burn(UInt160 account, BigInteger amount)
        {
            if (!IsOwner()) throw new InvalidOperationException("No Authorization!");
            Nep17Token.Burn(account, amount);
        }

        public static void Transfer(UInt160 from, UInt160 to, BigInteger amount)
        {
            if (!IsOwner()) throw new InvalidOperationException("No Authorization!");
            Nep17Token.Transfer(from, to, amount, null);
        }

        public static bool Update(ByteString nefFile, string manifest)
        {
            if (!IsOwner()) throw new InvalidOperationException("No Authorization!");
            ContractManagement.Update(nefFile, manifest, null);
            return true;
        }

        public static bool Destroy()
        {
            if (!IsOwner()) throw new InvalidOperationException("No Authorization!");
            ContractManagement.Destroy();
            return true;
        }

        public struct Voter {
            public int weight;
            public int votes;
        }

        public struct Location {
            public UInt160 owner;
            public int map;
            public string name;
            public string description;
            public string x;
            public string y;
            public string tags;
            public string image;
            public int voteCount;
        }

        public static Map<UInt160, Voter> voters = new Map<UInt160, Voter>();

        public static Map<int, Location> locations = new Map<int, Location>();

        public static int locationCount = 0;

        public static void allowToVote(UInt160 voter) {
            Voter sender = voters[voter];
            if (sender.weight == 0) throw new InvalidOperationException("Has no right to vote");
            if (!Runtime.CheckWitness(GetAdmin())) throw new InvalidOperationException("Only administrator can give right to vote.");
            if (sender.votes > 0) throw new InvalidOperationException("The voter already voted.");
            sender.weight = 1;
            voters[voter] = sender;
        }

        public static void vote(int location, UInt160 from) {
            Voter sender = voters[from];
            if (sender.weight == 0) throw new InvalidOperationException("Has no right to vote");
            if (sender.votes > 0) throw new InvalidOperationException("Already voted.");
            sender.votes = 1;
            voters[from] = sender;
            Location _loc = locations[location];
            _loc.voteCount += sender.weight;
            locations[location] = _loc;
        }

        public static UInt160 ownerOf(int location) {
            return locations[location].owner;
        }

        public static Location detailOf(int location) {
            return locations[location];
        }

        public static int getLocationCount() {
            return locationCount;
        }

        public static void post(
            UInt160 sender,
            int map,
            string name,
            string description,
            string x,
            string y,
            string tags,
            string image
        ) {
            if (!Runtime.CheckWitness(GetAdmin())) throw new InvalidOperationException("No Only admin can post new locations!");
            locations[locationCount] = new Location {
                owner = sender,
                map = map,
                name = name,
                description = description,
                x = x,
                y = y,
                tags = tags,
                image = image,
                voteCount = 0
            };
            locationCount += 1;
        }
    }
}
