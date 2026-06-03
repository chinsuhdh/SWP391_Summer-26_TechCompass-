import React from "react";
import { BrowserRouter } from "react-router-dom";
import MyNavbar from "./components/MyNavbar";
import "bootstrap/dist/css/bootstrap.min.css";
import { Navigate, Route, Routes, useLocation } from "react-router-dom";
import Home from "./pages/Home";
import 'bootstrap-icons/font/bootstrap-icons.css';
import Login from "./pages/Login";
import Register from "./pages/Register";


function App() {

  const location = useLocation();

  const noNavarPaths = ['/login', '/register'];
  const showNavbar = !noNavarPaths.includes(location.pathname);

  return (
    <div className="position-relative" style={{ backgroundColor: '#0a0a14' }}>
      {showNavbar && <MyNavbar />}
      
      <Routes>
         <Route index element={<Home />} /> {/* Đường dẫn mặc định khi truy cập vào "/" sẽ hiển thị Home */}
        <Route path="/" element={<Home />} />
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
      </Routes>
    </div>
  );
}

export default App;