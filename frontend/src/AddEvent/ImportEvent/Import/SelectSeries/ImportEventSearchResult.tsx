import React, { useCallback } from 'react';
import { useSelector } from 'react-redux';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import { icons } from 'Helpers/Props';
import createExistingSeriesSelector from 'Store/Selectors/createExistingSeriesSelector';
import ImportSeriesTitle from './ImportEventTitle';
import styles from './ImportEventSearchResult.css';

interface ImportSeriesSearchResultProps {
  tvdbId: number;
  title: string;
  year: number;
  network?: string;
  onPress: (tvdbId: number) => void;
}

function ImportSeriesSearchResult({
  tvdbId,
  title,
  year,
  network,
  onPress,
}: ImportSeriesSearchResultProps) {
  const isExistingSeries = useSelector(createExistingSeriesSelector(tvdbId));

  const handlePress = useCallback(() => {
    onPress(tvdbId);
  }, [tvdbId, onPress]);

  return (
    <div className={styles.container}>
      <Link className={styles.event} onPress={handlePress}>
        <ImportSeriesTitle
          title={title}
          year={year}
          network={network}
          isExistingSeries={isExistingSeries}
        />
      </Link>

      <Link
        className={styles.tvdbLink}
        to={`https://www.thetvdb.com/?tab=event&id=${tvdbId}`}
      >
        <Icon
          className={styles.tvdbLinkIcon}
          name={icons.EXTERNAL_LINK}
          size={16}
        />
      </Link>
    </div>
  );
}

export default ImportSeriesSearchResult;
