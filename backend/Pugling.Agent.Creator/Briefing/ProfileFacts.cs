namespace Pugling.Agent.Creator.Briefing;

/// <summary>
/// The <b>teacher</b> on whose behalf the design is made: subject, school branch, grade range, persona and didactics.
/// The textbook and unit are not the profile's: they are where the exercise is created (<see cref="MaterialFacts"/>, B-484).
/// <see cref="Persona"/> and <see cref="Didactics"/> deliberately go into the <b>system</b> prompt (they
/// describe who is designing), everything else into the request (it describes what is being designed).
/// </summary>
public sealed record ProfileFacts(
    int ProfileId,
    string Name,
    string? SubjectName,
    SchoolTypes SchoolTypes,
    int? GradeMin,
    int? GradeMax,
    string SourceLang,
    string TargetLang,
    string? Persona,
    string? Didactics);
