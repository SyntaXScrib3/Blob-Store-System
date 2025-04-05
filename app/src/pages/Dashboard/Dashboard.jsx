import { useState, useEffect, useRef, useCallback } from "react";
import { Container, Row, Col, Dropdown, Button, Card, Form } from "react-bootstrap";
import { useNavigate } from "react-router-dom";

import { removeToken, isAuthenticated } from "../../services/auth.js";
import api from "../../services/api.js";

import PathNavigator from "./components/PathNavigator.jsx";
import MoveCopyModal from "./components/modals/MoveCopyModal.jsx";
import CreateFolderModal from "./components/modals/CreateFolderModal.jsx";
import RenameModal from "./components/modals/RenameModal.jsx";
import PreviewModal from "./components/modals/PreviewModal.jsx";

/**
 * Main Dashboard page component
 */
function DashboardPage() {
   const navigate = useNavigate();

   // Current directory path and its contents
   const [path, setPath] = useState("/");
   const [contents, setContents] = useState([]);

   // Selected items in the current directory listing
   const [selectedIds, setSelectedIds] = useState([]);

   // For "Create Folder"
   const [showCreateFolder, setShowCreateFolder] = useState(false);

   // For "Rename"
   const [showRename, setShowRename] = useState(false);
   const [renameData, setRenameData] = useState({
      path: "",
      name: "",
      isDirectory: false,
   });

   // For "Preview" (file reading)
   const [showPreview, setShowPreview] = useState(false);
   const [previewData, setPreviewData] = useState({
      content: null,
      fileType: "",
      fileName: "",
   });

   // For "Upload File" hidden input
   const fileInputRef = useRef(null);

   // For "Move/Copy Modal"
   const [showSmallPanel, setShowSmallPanel] = useState(false);
   const [actionMode, setActionMode] = useState("move"); // or "copy"

   // Authenticated axios instance
   const authAxios = api.createAuthAxios(() => navigate("/login"));

   /**
    * =======================================
    * ---     List directory contents     ---
    * =======================================
    */
   const listDirectory = useCallback(
      async (dirPath) => {
         try {
            const resp = await api.List(dirPath, authAxios);
            const sortedData = resp.data.sort((a, b) => {
               // Sort by mimeType first, then by name
               if (a.mimeType !== b.mimeType) {
                  return a.mimeType.localeCompare(b.mimeType);
               }
               return a.name.localeCompare(b.name);
            });
            setContents(sortedData);
            setSelectedIds([]);
         } catch (err) {
            console.error("Error listing directory:", err);
         }
      },
      [authAxios]
   );

   useEffect(() => {
      if (!isAuthenticated()) {
         navigate("/login");
         return;
      }
      listDirectory(path);
   }, [path]);

   // ----------------------------------------------
   // BREADCRUMB
   // ----------------------------------------------
   /*const pathSegments = getPathSegments(path);
   const maxSegmentsToShow = 4;
   const { visibleSegments, hiddenSegments } = buildBreadcrumbDisplay(pathSegments, maxSegmentsToShow);

   const buildNewPath = (targetIndex) => {
       const subSegments = pathSegments.slice(0, targetIndex + 1);
       return "/" + subSegments.join("/");
   };

   const handleSegmentClick = (segIndex) => {
       if (segIndex === -1) {
           setPath("/");
           return;
       }
       if (segIndex >= 0) {
           const newPath = buildNewPath(segIndex);
           setPath(newPath);
       }
   };*/

   /**
    * =======================================
    * ---     File/Folder Selection     ---
    * =======================================
    */
   const toggleSelection = useCallback(
      (id) => {
         setSelectedIds((prev) =>
            prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]
         );
      },
      [setSelectedIds]
   );

   const isSelected = useCallback(
      (id) => selectedIds.includes(id),
      [selectedIds]
   );

   const hasSelected = selectedIds.length > 0;
   const selectedNodes = contents.filter((node) => selectedIds.includes(node.id));
   const onlyFilesSelected = selectedNodes.every((node) => !node.isDirectory);
   const singleSelection = selectedIds.length === 1;

   /**
    * =============================
    * ---     Create Folder     ---
    * =============================
    */
   const handleCreateFolder = useCallback(
      async (folderName) => {
         if (!folderName.trim()) return;
         const base = path.endsWith("/") ? path : path + "/";
         const target = base + folderName;
         try {
            await api.Create(target, authAxios);
            setShowCreateFolder(false);
            listDirectory(path);
         } catch (err) {
            console.error("Error creating folder:", err);
         }
      },
      [path, authAxios, listDirectory]
   );

   /**
    * ===========================
    * ---     Upload File     ---
    * ===========================
    */
   const handleUploadClick = useCallback(() => {
      if (fileInputRef.current) {
         fileInputRef.current.click();
      }
   }, []);

   const handleUploadFile = useCallback(
      async (e) => {
         const file = e.target.files[0];
         if (!file) return;

         const base = path.endsWith("/") ? path : path + "/";
         const target = base + file.name;

         try {
            const formData = new FormData();
            formData.append("file", file);
            await api.Upload(target, formData, authAxios);

            // Reset the file input so user can upload the same file name if needed
            e.target.value = null;

            listDirectory(path);
         } catch (err) {
            console.error("Error uploading file:", err);
         }
      },
      [path, authAxios, listDirectory]
   );

   /**
    * ============================
    * ---     File Preview     ---
    * ============================
    */
   const handlePreviewFile = useCallback(
      async (node) => {
         if (node.isDirectory) {
            // Navigate into directory
            setPath(node.path);
            return;
         }
         try {
            const resp = await api.Download(node.path, authAxios);
            const mime =
               resp.headers["content-type"] || node.mimeType || "application/octet-stream";

            const blob = new Blob([resp.data], { type: mime });
            let previewUrl = null;
            let previewText = null;

            if (mime.startsWith("image/")) {
               // For images, create an object URL
               previewUrl = URL.createObjectURL(blob);
            } else if (mime.startsWith("text/")) {
               // For text, decode the bytes into a string
               const decoder = new TextDecoder("utf-8");
               previewText = decoder.decode(resp.data);
            } else {
               // For other files, still create an object URL to allow a download
               previewUrl = URL.createObjectURL(blob);
            }

            setPreviewData({
               content: previewUrl ?? previewText,
               fileType: mime,
               fileName: node.name,
            });
            setShowPreview(true);
         } catch (err) {
            console.error("Error previewing file:", err);
         }
      },
      [authAxios]
   );

   const closePreview = useCallback(() => {
      const { content, fileType } = previewData;

      // Revoke the object URL for images or other object URLs
      if (content && fileType.startsWith("image/")) {
         URL.revokeObjectURL(content);
      }

      setShowPreview(false);
      setPreviewData({ content: null, fileType: "", fileName: "" });
   }, [previewData]);


   /**
    * ==================================
    * ---     Delete File/Folder     ---
    * ==================================
    */
   const handleDelete = useCallback(async () => {
      for (const node of selectedNodes) {
         try {
            if (node.isDirectory) {
               await api.Delete("directory", node.path, authAxios);
            } else {
               await api.Delete("file", node.path, authAxios);
            }
         } catch (err) {
            console.error("Error deleting:", err);
         }
      }
      listDirectory(path);
   }, [selectedNodes, authAxios, listDirectory, path]);

   /**
    * =============================
    * ---     Download File     ---
    * =============================
    */
   const handleDownload = useCallback(async () => {
      for (const node of selectedNodes) {
         if (!node.isDirectory) {
            try {
               const resp = await api.Download(node.path, authAxios);
               const mime = node.mimeType || "application/octet-stream";
               const blob = new Blob([resp.data], { type: mime });
               const url = URL.createObjectURL(blob);

               // Trigger browser download
               const a = document.createElement("a");
               a.href = url;
               a.download = node.name;
               document.body.appendChild(a);
               a.click();
               a.remove();
               URL.revokeObjectURL(url);
            } catch (err) {
               console.error("Error downloading file:", err);
            }
         }
      }
   }, [selectedNodes, authAxios]);

   /**
    * =========================
    * ---     Move/Copy     ---
    * =========================
    */
   const openSmallPanel = useCallback(
      (mode) => {
         setActionMode(mode); // "move" or "copy"
         setShowSmallPanel(true);
      },
      [setActionMode, setShowSmallPanel]
   );

   const buildTargetPath = (destinationPath, nodeName) => {
      let final = destinationPath.endsWith("/")
         ? destinationPath.slice(0, -1)
         : destinationPath;

      const segments = final.split("/").filter(Boolean);
      const lastSegment = segments.length ? segments[segments.length - 1] : null;

      if (lastSegment && lastSegment === nodeName) {
         return final;
      } else {
         return final + "/" + nodeName;
      }
   };

   const handleMoveCopy = useCallback(
      async (destinationPath) => {
         for (const node of selectedNodes) {
            try {
               const newPath = buildTargetPath(destinationPath, node.name);
               await api.Move_Copy(node.isDirectory ? "directory" : "file", actionMode, node.path, newPath, authAxios);
            } catch (err) {
               console.error(`Error on ${actionMode} action:`, err);
            }
         }
         setShowSmallPanel(false);
         listDirectory(path);
      },
      [selectedNodes, actionMode, authAxios, listDirectory, path]
   );

   /**
    * ==================================
    * ---     Rename File/Folder     ---
    * ==================================
    */
   const openRenameModal = useCallback(() => {
      if (selectedNodes.length !== 1) return; // safety check
      const node = selectedNodes[0];
      setRenameData({
         path: node.path,
         name: node.name,
         isDirectory: node.isDirectory,
      });
      setShowRename(true);
   }, [selectedNodes]);

   const handleRenameConfirm = useCallback(
      async (newName) => {
         try {
            await api.Move_Copy(renameData.isDirectory ? "directory" : "file", renameData.path, newName, authAxios);

            listDirectory(path);
            setShowRename(false);
         } catch (err) {
            console.error("Rename failed:", err);
            alert(err.response?.data || "Rename failed");
         }
      },
      [renameData, authAxios, listDirectory, path]
   );

   /**
    * ======================
    * ---     Logout     ---
    * ======================
    */
   const handleLogout = () => {
      removeToken();
      navigate("/login");
   };

   return (
      <Container fluid className="vh-100 d-flex flex-column">
         {/* TOP BAR */}
         <Row className="py-3 px-4 bg-light align-items-center">
            {/* Add New Dropdown Button */}
            <Col xs="auto">
               <Dropdown>
                  <Dropdown.Toggle variant="primary" id="addNewDropdown">
                     Add New
                  </Dropdown.Toggle>

                  <Dropdown.Menu>
                     <Dropdown.Item onClick={() => setShowCreateFolder(true)}>
                        Create Folder
                     </Dropdown.Item>

                     <Dropdown.Item onClick={handleUploadClick}>
                        Upload File
                     </Dropdown.Item>
                  </Dropdown.Menu>
               </Dropdown>
            </Col>

            {/* Action Toolbar */}
            <Col xs="auto">
               {hasSelected && (
                  <>
                     {onlyFilesSelected && (
                        <Button variant="info" className="me-2" onClick={handleDownload}>
                           Download
                        </Button>
                     )}

                     <Button
                        variant="secondary"
                        className="me-2"
                        onClick={() => openSmallPanel("move")}
                     >
                        Move To
                     </Button>
                     <Button
                        variant="success"
                        className="me-2"
                        onClick={() => openSmallPanel("copy")}
                     >
                        Copy
                     </Button>
                     <Button variant="danger" className="me-2" onClick={handleDelete}>
                        Delete
                     </Button>

                     {singleSelection && (
                        <Button variant="warning" className="me-2" onClick={openRenameModal}>
                           Rename
                        </Button>
                     )}
                  </>
               )}
            </Col>

            <Col className="flex-grow-1" />

            <Col xs="auto">
               <Button variant="outline-danger" onClick={handleLogout}>
                  Logout
               </Button>
            </Col>
         </Row>

         <PathNavigator
            path={path}
            onNavigate={setPath}
         />

         {/* MAIN FILE WINDOW */}
         <Row className="flex-grow-1">
            <Col className="p-3">
               <Card className="h-100" style={{ backgroundColor: "#f8f9fa" }}>
                  <Card.Body>
                     {contents.map((node) => (
                        <div
                           key={node.id}
                           className="d-flex align-items-center border-bottom py-2"
                           style={{ cursor: "pointer" }}
                        >
                           <Form.Check
                              className="me-2"
                              checked={isSelected(node.id)}
                              onChange={() => toggleSelection(node.id)}
                           />
                           <div
                              onClick={() => handlePreviewFile(node)}
                              style={{ flexGrow: 1 }}
                           >
                              <strong>{node.name}</strong>
                              {node.isDirectory && (
                                 <span className="text-muted ms-2">(dir)</span>
                              )}
                           </div>
                        </div>
                     ))}
                  </Card.Body>
               </Card>
            </Col>
         </Row>

         {/* CREATE FOLDER MODAL */}
         <CreateFolderModal
            show={showCreateFolder}
            onHide={() => setShowCreateFolder(false)}
            onCreate={handleCreateFolder}
         />

         {/* RENAME MODAL */}
         <RenameModal
            show={showRename}
            isDirectory={renameData.isDirectory}
            originalName={renameData.name}
            onHide={() => setShowRename(false)}
            onConfirm={handleRenameConfirm}
         />

         {/* PREVIEW MODAL */}
         <PreviewModal
            show={showPreview}
            previewData={previewData}
            onClose={closePreview}
         />

         {/* MOVE/COPY MODAL */}
         {showSmallPanel && (
            <MoveCopyModal
               show={showSmallPanel}
               mode={actionMode}
               onClose={() => setShowSmallPanel(false)}
               onComplete={handleMoveCopy}
               authAxios={authAxios}
               selectedNodes={selectedNodes}
            />
         )}

         {/* Hidden file input */}
         <input
            type="file"
            ref={fileInputRef}
            style={{ display: "none" }}
            onChange={handleUploadFile}
         />
      </Container>
   );
}

export default DashboardPage;


