/**
 * Converts a given path string into an array of breadcrumb items.
 *
 * @param {string} path - The directory path (e.g., "/FolderA/SubFolderA").
 * @returns {Array<{ label: string, path: string }>} Array of breadcrumb objects.
 */
function getPathItems(path) {
    const items = [];
    items.push({ label: "My Files", path: "/" });

    if (path === "/" || !path) {
        return items;
    }

    // Split the path into segments and build up the breadcrumb items.
    const segments = path.split("/").filter(Boolean);
    let currentPath = "";
    segments.forEach((segment) => {
        currentPath += `/${segment}`;
        items.push({ label: segment, path: currentPath });
    });

    return items;
}

export default getPathItems;
