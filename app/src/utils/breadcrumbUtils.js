// utils/breadcrumbUtils.js

/**
 * Split a path like "/TestDir/SubDir/SubSubDir" into segments:
 * [ "TestDir", "SubDir", "SubSubDir" ]
 * If path === "/", return empty array (meaning root).
 */
export function getPathSegments(path) {
    if (!path || path === "/") return [];
    return path.split("/").filter(Boolean);
}

/**
 * Build a displayed breadcrumb model:
 * We only show up to `maxSegmentsToShow`.
 * If actual segments > maxSegmentsToShow, we collapse the middle ones into "..."
 *
 * Example:
 *   segments = ["TestDir", "SubTestDir", "Deep", "Deeper", "EvenDeeper"]
 *   maxSegmentsToShow = 3
 *   => we show [ "TestDir" , "..." , "EvenDeeper" ]
 *   The hidden in "..." are [ "SubTestDir", "Deep", "Deeper" ]
 */
export function buildBreadcrumbDisplay(segments, maxSegmentsToShow = 3) {
    const total = segments.length;
    if (total <= maxSegmentsToShow) {
        // Show all
        return {
            visibleSegments: segments.map((seg, idx) => ({ name: seg, index: idx })),
            hiddenSegments: []
        };
    }

    // We'll show the first segment, the last segment, and collapse the middle
    const first = { name: segments[0], index: 0 };
    const last = { name: segments[total - 1], index: total - 1 };
    const middle = segments.slice(1, total - 1).map((seg, idx) => ({
        name: seg,
        index: idx + 1 // offset
    }));

    return {
        visibleSegments: [first, { name: "...", index: -1 }, last],
        hiddenSegments: middle
    };
}