using AAFSS.Core.Models;

namespace AAFSS.Core.Specifications;

/// <summary>
/// Specification that matches data sources by their last processing step status.
/// A data source's status is determined by the status of its most recent
/// processing step (by StepOrder). Data sources with no steps are considered Pending.
/// </summary>
public class DataSourceByStatusSpec : Specification<DataSource>
{
    private readonly ProcessingStatus _targetStatus;

    public DataSourceByStatusSpec(ProcessingStatus targetStatus)
    {
        _targetStatus = targetStatus;
    }

    public override bool IsSatisfiedBy(DataSource candidate)
    {
        if (candidate.ProcessingSteps == null || candidate.ProcessingSteps.Count == 0)
        {
            return _targetStatus == ProcessingStatus.Pending;
        }

        var lastStep = candidate.ProcessingSteps
            .OrderBy(s => s.StepOrder)
            .Last();

        return lastStep.Status == _targetStatus;
    }
}
