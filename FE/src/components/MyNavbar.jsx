import React, { useState, useEffect } from 'react';
import Container from 'react-bootstrap/Container';
import Nav from 'react-bootstrap/Nav';
import Navbar from 'react-bootstrap/Navbar';
import Button from 'react-bootstrap/Button';
import { Link } from 'react-router-dom'; // Thay đổi ở đây

function MyNavbar() {
  const [isScrolled, setIsScrolled] = useState(false);

  useEffect(() => {
    const handleScroll = () => {
      if (window.scrollY > 50) {
        setIsScrolled(true);
      } else {
        setIsScrolled(false);
      }
    };
    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  return (
    <>
      <style>{`
        @media (max-width: 991.98px) {
          .navbar-collapse {
            background-color: #0a0a14 !important;
            padding: 1.5rem;
            border-radius: 12px;
            margin-top: 10px;
            box-shadow: 0 10px 30px rgba(0,0,0,0.5);
            border: 1px solid rgba(255, 255, 255, 0.08);
          }
        }
      `}</style>

      <Navbar
        expand="lg"
        variant="dark"
        className="fixed-top w-100 start-0 top-0"
        style={{
          zIndex: 1050,
          backgroundColor: isScrolled ? '#0a0a14' : 'transparent',
          backdropFilter: isScrolled ? 'blur(8px)' : 'none',
          borderBottom: isScrolled ? '1px solid rgba(255, 255, 255, 0.08)' : 'none',
          transition: 'all 0.3s ease-in-out'
        }}
      >
        <Container>
          <Navbar.Brand href="#home" className="fw-bold text-white fs-4">
              <span style={{
                background: 'linear-gradient(to right, #00bfa5 0%, #00bfa5 30%, #ffffff 70%, #ffffff 100%)',
                WebkitBackgroundClip: 'text',
                WebkitTextFillColor: 'transparent',
                display: 'inline-block',
                fontWeight: '900',
                fontSize: '1.25rem'

              }}>
                AICareer
              </span>
          </Navbar.Brand>

          <Navbar.Toggle aria-controls="basic-navbar-nav" className="border-secondary" />

          <Navbar.Collapse id="basic-navbar-nav">
            <Nav className="mx-auto gap-3 my-2 my-lg-0">
              <Nav.Link href="#features" className="text-white opacity-75">Tính năng</Nav.Link>
              <Nav.Link href="#how-it-works" className="text-white opacity-75">Cách hoạt động</Nav.Link>
              <Nav.Link href="#mentors" className="text-white opacity-75">Mentor</Nav.Link>
              <Nav.Link href="#about" className="text-white opacity-75">Về chúng tôi</Nav.Link>
            </Nav>

            <Nav className="ms-auto d-flex flex-column flex-lg-row align-items-start align-items-lg-center gap-3 text-nowrap">
              <Nav.Link as={Link} to="/login" className="text-white fw-medium">
                Đăng nhập
              </Nav.Link>
              
              <Button
                as={Link} to="/register"
                className="px-4 py-2 fw-semibold rounded-pill w-100 w-lg-auto text-center"
                style={{ backgroundColor: '#10b981', border: 'none', color: '#0a0a14' }}
              >
                Bắt đầu miễn phí
              </Button>
            </Nav>
          </Navbar.Collapse>
        </Container>
      </Navbar>
    </>
  );
}

export default MyNavbar;
