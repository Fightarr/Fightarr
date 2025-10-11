import React from 'react';
import EventMonitoringOptionsPopoverContent from 'AddEvent/EventMonitoringOptionsPopoverContent';
import EventTypePopoverContent from 'AddEvent/EventTypePopoverContent';
import Icon from 'Components/Icon';
import VirtualTableHeader from 'Components/Table/VirtualTableHeader';
import VirtualTableHeaderCell from 'Components/Table/VirtualTableHeaderCell';
import VirtualTableSelectAllHeaderCell from 'Components/Table/VirtualTableSelectAllHeaderCell';
import Popover from 'Components/Tooltip/Popover';
import { icons, tooltipPositions } from 'Helpers/Props';
import { CheckInputChanged } from 'typings/inputs';
import translate from 'Utilities/String/translate';
import styles from './ImportEventHeader.css';

interface ImportSeriesHeaderProps {
  allSelected: boolean;
  allUnselected: boolean;
  onSelectAllChange: (change: CheckInputChanged) => void;
}

function ImportSeriesHeader({
  allSelected,
  allUnselected,
  onSelectAllChange,
}: ImportSeriesHeaderProps) {
  return (
    <VirtualTableHeader>
      <VirtualTableSelectAllHeaderCell
        allSelected={allSelected}
        allUnselected={allUnselected}
        onSelectAllChange={onSelectAllChange}
      />

      <VirtualTableHeaderCell className={styles.folder} name="folder">
        {translate('Folder')}
      </VirtualTableHeaderCell>

      <VirtualTableHeaderCell className={styles.monitor} name="monitor">
        {translate('Monitor')}

        <Popover
          anchor={<Icon className={styles.detailsIcon} name={icons.INFO} />}
          title={translate('MonitoringOptions')}
          body={<EventMonitoringOptionsPopoverContent />}
          position={tooltipPositions.RIGHT}
        />
      </VirtualTableHeaderCell>

      <VirtualTableHeaderCell
        className={styles.qualityProfile}
        name="qualityProfileId"
      >
        {translate('QualityProfile')}
      </VirtualTableHeaderCell>

      <VirtualTableHeaderCell className={styles.seriesType} name="seriesType">
        {translate('SeriesType')}

        <Popover
          anchor={<Icon className={styles.detailsIcon} name={icons.INFO} />}
          title={translate('SeriesType')}
          body={<EventTypePopoverContent />}
          position={tooltipPositions.RIGHT}
        />
      </VirtualTableHeaderCell>

      <VirtualTableHeaderCell
        className={styles.seasonFolder}
        name="seasonFolder"
      >
        {translate('SeasonFolder')}
      </VirtualTableHeaderCell>

      <VirtualTableHeaderCell className={styles.event} name="event">
        {translate('Event')}
      </VirtualTableHeaderCell>
    </VirtualTableHeader>
  );
}

export default ImportSeriesHeader;
