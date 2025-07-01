using AutoMapper;

using Newtonsoft.Json;

using XtremeIdiots.Portal.DataLib;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.AdminActions;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.BanFileMonitors;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.ChatMessages;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Demos;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.MapPacks;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Maps;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.RecentPlayers;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Reports;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Tags;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.UserProfiles;
using XtremeIdiots.Portal.RepositoryWebApi.V1.Extensions;

namespace XtremeIdiots.Portal.RepositoryWebApi.V1.AutoMapProfiles
{
    public class DbModelsProfile : Profile
    {
        public DbModelsProfile()
        {
            // Nullable Handling
            CreateMap<int?, int>().ConvertUsing((src, dest) => src ?? dest);
            CreateMap<int?, int?>().ConvertUsing((src, dest) => src ?? dest);
            CreateMap<long?, long>().ConvertUsing((src, dest) => src ?? dest);
            CreateMap<long?, long?>().ConvertUsing((src, dest) => src ?? dest);
            CreateMap<bool?, bool>().ConvertUsing((src, dest) => src ?? dest);
            CreateMap<bool?, bool?>().ConvertUsing((src, dest) => src ?? dest);
            CreateMap<string?, string>().ConvertUsing((src, dest) => src ?? dest);
            CreateMap<string?, string?>().ConvertUsing((src, dest) => src ?? dest);
            CreateMap<double?, double>().ConvertUsing((src, dest) => src ?? dest);
            CreateMap<double?, double?>().ConvertUsing((src, dest) => src ?? dest);
            CreateMap<Guid?, Guid>().ConvertUsing((src, dest) => src ?? dest);
            CreateMap<Guid?, Guid?>().ConvertUsing((src, dest) => src ?? dest);
            CreateMap<DateTime?, DateTime>().ConvertUsing((src, dest) => src ?? dest);
            CreateMap<DateTime?, DateTime?>().ConvertUsing((src, dest) => src ?? dest);

            // AdminActions
            CreateMap<AdminAction, AdminActionDto>()
                .ForMember(
                    dest => dest.Type,
                    src => src.MapFrom(src => src.Type.ToAdminActionType())
                );

            CreateMap<CreateAdminActionDto, AdminAction>();

            CreateMap<EditAdminActionDto, AdminAction>();

            // Ban File Monitors
            CreateMap<BanFileMonitor, BanFileMonitorDto>();

            CreateMap<CreateBanFileMonitorDto, BanFileMonitor>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<EditBanFileMonitorDto, BanFileMonitor>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Chat Messages
            CreateMap<ChatMessage, ChatMessageDto>()
                .ForMember(
                    dest => dest.ChatType,
                    src => src.MapFrom(src => src.ChatType.ToChatType())
                );

            CreateMap<CreateChatMessageDto, ChatMessage>()
                .ForMember(
                    dest => dest.ChatType,
                    src => src.MapFrom(src => src.ChatType.ToChatTypeInt())
                )
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Demos
            CreateMap<Demo, DemoDto>()
                .ForMember(
                    dest => dest.GameType,
                    src => src.MapFrom(src => src.GameType.ToGameType())
                );

            CreateMap<CreateDemoDto, Demo>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Game Servers
            CreateMap<GameServer, GameServerDto>()
                .ForMember(
                    dest => dest.GameType,
                    src => src.MapFrom(src => src.GameType.ToGameType())
                );

            CreateMap<CreateGameServerDto, GameServer>()
                .ForMember(
                    dest => dest.GameType,
                    src => src.MapFrom(src => src.GameType.ToGameTypeInt())
                )
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<EditGameServerDto, GameServer>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Game Server Stats
            CreateMap<GameServerStat, GameServerStatDto>();

            CreateMap<CreateGameServerStatDto, GameServerStat>();

            CreateMap<CreateGameServerEventDto, GameServerEvent>();

            // Live Players
            CreateMap<LivePlayer, LivePlayerDto>()
                .ForMember(
                    dest => dest.GameType,
                    src => src.MapFrom(src => src.GameType.ToGameType())
                );

            CreateMap<CreateLivePlayerDto, LivePlayer>()
                .ForMember(
                    dest => dest.GameType,
                    src => src.MapFrom(src => src.GameType.ToGameTypeInt())
                )
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Maps
            CreateMap<Map, MapDto>()
                .ForMember(
                    dest => dest.GameType,
                    src => src.MapFrom(src => src.GameType.ToGameType())
                )
                .ForMember(
                    dest => dest.MapFiles,
                    src => src.MapFrom(src => JsonConvert.DeserializeObject<List<MapFileDto>>(src.MapFiles))
                );

            CreateMap<CreateMapDto, Map>()
                .ForMember(
                    dest => dest.GameType,
                    src => src.MapFrom(src => src.GameType.ToGameTypeInt())
                )
                .ForMember(
                    dest => dest.MapFiles,
                    src => src.MapFrom(src => JsonConvert.SerializeObject(src.MapFiles))
                )
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<EditMapDto, Map>()
                .ForMember(
                    dest => dest.MapFiles,
                    src => src.MapFrom(src => JsonConvert.SerializeObject(src.MapFiles))
                )
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<MapVote, MapVoteDto>();
            CreateMap<UpsertMapVoteDto, MapVote>();

            // Map Packs
            CreateMap<MapPack, MapPackDto>();

            CreateMap<CreateMapPackDto, MapPack>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<UpdateMapPackDto, MapPack>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));            // Players
            CreateMap<Player, PlayerDto>()
                .ForMember(
                    dest => dest.GameType,
                    src => src.MapFrom(src => src.GameType.ToGameType())
                );

            CreateMap<PlayerAlias, AliasDto>();
            CreateMap<PlayerAlias, PlayerAliasDto>();
            CreateMap<PlayerIpAddress, IpAddressDto>();

            CreateMap<PlayerIpAddress, RelatedPlayerDto>()
                .ForMember(
                    dest => dest.GameType,
                    src => src.MapFrom(src => src.Player.GameType.ToGameType())
                )
                .ForMember(
                    dest => dest.Username,
                    src => src.MapFrom(src => src.Player.Username)
                )
                .ForMember(
                    dest => dest.PlayerId,
                    src => src.MapFrom(src => src.Player.PlayerId)
                )
                .ForMember(
                    dest => dest.IpAddress,
                    src => src.MapFrom(src => src.Address)
                );

            CreateMap<CreatePlayerDto, Player>()
                .ForMember(
                    dest => dest.GameType,
                    src => src.MapFrom(src => src.GameType.ToGameTypeInt())
                )
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Protected Names
            CreateMap<ProtectedName, ProtectedNameDto>();
            CreateMap<CreateProtectedNameDto, ProtectedName>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Recent Players
            CreateMap<RecentPlayer, RecentPlayerDto>()
                .ForMember(
                    dest => dest.GameType,
                    src => src.MapFrom(src => src.GameType.ToGameType())
                );

            CreateMap<CreateRecentPlayerDto, RecentPlayer>()
                .ForMember(
                    dest => dest.GameType,
                    src => src.MapFrom(src => src.GameType.ToGameTypeInt())
                )
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Reports
            CreateMap<Report, ReportDto>()
                .ForMember(
                    dest => dest.GameType,
                    src => src.MapFrom(src => src.GameType.ToGameType())
                );

            CreateMap<CreateReportDto, Report>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Tags
            CreateMap<Tag, TagDto>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<TagDto, Tag>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // PlayerTags
            CreateMap<PlayerTag, PlayerTagDto>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<PlayerTagDto, PlayerTag>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // User Profile
            CreateMap<UserProfile, UserProfileDto>();

            CreateMap<UserProfileClaim, UserProfileClaimDto>();

            CreateMap<EditUserProfileDto, UserProfile>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<CreateUserProfileDto, UserProfile>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<CreateUserProfileClaimDto, UserProfileClaim>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
