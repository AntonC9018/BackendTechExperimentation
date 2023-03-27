﻿using AutoMapper;

namespace efcore_transactions;

#pragma warning disable CS8618

using ID = Int64;

public class PersonRequestDto
{
    public ID? Id { get; set; }
    public string Name { get; set; }
    public List<PersonProjectRequestDto> Projects { get; set; } = new();
}

public class PersonProjectRequestDto
{
    public ID? Id { get; set; }
    public string ProjectName { get; set; }
}

public class PersonResponseDto
{
    public ID Id { get; set; }
    public string Name { get; set; }
    public List<PersonProjectResponseDto> Projects { get; set; } = new();
}

public class PersonProjectResponseDto
{
    public ID Id { get; set; }
    public string ProjectName { get; set; }
}

public class ProjectRequestDto
{
    public ID? Id { get; set; }
    public ID PersonId { get; set; }
    public string ProjectName { get; set; }
}

public class ProjectResponseDto
{
    public ID Id { get; set; }
    public ID PersonId { get; set; }
    public string ProjectName { get; set; }
}

public class NameDto
{
    public string Name { get; set; }
}

public class GraphQlPersonDto
{
    public ID Id { get; set; }
    public string Name { get; set; }
    
    // Note: intentionally misspelt
    public List<GraphQlProjectDto> Projets { get; set; } = new();
}

public class GraphQlProjectDto
{
    public ID Id { get; set; }
    public string Name { get; set; }
}

#pragma warning restore CS8618


public static class AutoMapperExtensions
{
    public static IMappingExpression<TSource, TDestination> UnmapAllNulls<TSource, TDestination>(this IMappingExpression<TSource, TDestination> expression)
    {
        expression.ForAllMembers(opts => 
            opts.Condition((_, _, srcMember) => srcMember is not null));
        return expression;
    }
}

public class MapperProfile : Profile
{
    void CreateNonNullableToDefaultMap<T>() where T : struct
    {
        CreateMap<T?, T>().ConvertUsing((src, dest) => src ?? dest);
    }
    
    public MapperProfile()
    {
        CreateNonNullableToDefaultMap<int>();
        CreateNonNullableToDefaultMap<long>();
        
        CreateMap<PersonRequestDto, Person>(MemberList.Source)
            .UnmapAllNulls();
        CreateMap<PersonProjectRequestDto, Project>(MemberList.Source)
            .UnmapAllNulls();
        
        CreateMap<Person, PersonResponseDto>(MemberList.Destination);
        CreateMap<Project, PersonProjectResponseDto>(MemberList.Destination);
        
        CreateMap<ProjectRequestDto, Project>(MemberList.Source)
            .UnmapAllNulls();
        CreateMap<Project, ProjectResponseDto>(MemberList.Destination);
        
        CreateMap<Project, NameDto>(MemberList.Destination)
            .ForMember(d => d.Name, o => o.MapFrom(s => s.ProjectName))
            .ReverseMap();

        {
            CreateMap<GraphQlPersonDto, Person>(MemberList.Source)
                .ForMember(d => d.Projects, o => o.MapFrom(s => s.Projets))
                .ReverseMap()
                // .ForAllMembers(opt => opt.ExplicitExpansion())
                ;
        }
        {
            CreateMap<GraphQlProjectDto, Project>(MemberList.Source)
                .ForMember(d => d.ProjectName, o => o.MapFrom(s => s.Name))
                .ReverseMap()
                .ForAllMembers(opt => opt.ExplicitExpansion())
                ;
        }
    }
}
