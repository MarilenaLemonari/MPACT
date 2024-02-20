---
layout: defaultPaper
title: P2C - A Paths-to-Crowds Framework to Parameterize Behaviors
permalink: /
---

<!-- <center>
<figure style="display:inline-block;margin:0;padding:0"><img src="assets/img/teaser.jpg" alt="teaser" width="100%" /><figcaption style="text-align:center"></figcaption></figure>
</center> -->

<br>

<div class="img_horizontal_container">
	<a href="https://doi.org/10.36227/techrxiv.170654693.38725484/v1">
	<div class="img-with-text">
		<img src="assets/img/article_icon.svg" alt="paper" />
		<p><b>Paper (Preprint)</b></p>
	</div>
	</a>
	<!-- <a href="https://github.com/UPC-ViRVIG/SparsePoser">
	<div class="img-with-text">
		<img src="assets/img/github_icon.svg" alt="code" />
		<p><b>Code</b></p>
	</div>
	</a> -->
	<a href="https://youtu.be/5BKo8Tv9Sps">
	<div class="img-with-text">
		<img src="assets/img/video_icon.svg" alt="video" />
		<p><b>Video</b></p>
	</div>
	</a>
</div>

------

<h3><center><b>
Abstract
</b></center></h3>

<div style="text-align: justify;">
Simulating believable crowds heavily relies upon the perceived realism and diversity of the agents' behaviors, whilst generating novel crowd simulations highly depends on being able to easily manipulate intuitive behavior parameters. We present P2C (Paths-to-Crowds), a method that parameterizes reference crowd data and enables the simulation of similar behaviors in different environments. This approach enables explainable and fine-grained control of simulations since artists can modify at run-time a small set of intuitive parameters in local regions. We incorporate the integration of four fundamental behaviors: goal-seeking, grouping, interaction with areas of interest, and connectivity-into an existing Reinforcement Learning (RL)-based crowd simulation system, which facilitates the creation of customizable agent behaviors and interactions. To learn a parameter model, we synthesize numerous simulations by sampling the parameter space; we then use these data to learn a model that outputs the underlying parameters. To achieve this, we encode the simulation in a set of 2D maps that encode different measurements such as velocities, occupancy, interpersonal distances, path deviations, etc. The trained model can then be used to infer parameters in localized regions of given crowd data (both real and simulated). This approach enables replication of behavior, transfer to new environments, real-time local control, editing of parameters, and explainability of behaviors (with respect to the fundamental behaviors). We evaluate our model's predictive power on real data and compare it against existing baselines. This along with the accompanying user study reveals P2C's potential in terms of achieving behavior realism and diversity.</div>

------

<div class="row">
	<figure style="display:inline-block;margin:0;padding:0">
		<video width="100%" autoplay muted loop>
		  <source src="assets/img/intro.mp4" type="video/mp4">
		Your browser does not support the video tag.
		</video>
	</figure>
</div>

--------

<h3><center><b>
Overview Video
</b></center></h3>

<center>
<div class="video_wrapper">
	<iframe frameborder="0" width="100%" height="100%"
	src="https://youtu.be/5BKo8Tv9Sps">
	</iframe>
</div>
</center>

-----

<br>

<h3><b>
Citation
</b></h3>
<div style="background-color:rgba(0, 0, 0, 0.03); vertical-align: middle; padding:10px 20px;">
@article{LemonariPanayiotou_2024, <br>
	title={P2C: A Paths-to-Crowds Framework to Parameterize Behaviors}, <br>
	url={http://dx.doi.org/10.36227/techrxiv.170654693.38725484/v1}, <br>
	DOI={10.36227/techrxiv.170654693.38725484/v1},  <br>
	publisher={Institute of Electrical and Electronics Engineers (IEEE)}, <br>
	author={Lemonari, Marilena and Panayiotou, Andreas and Pelechano, Nuria and Kyriakou, Theodoros and Chrysanthou, Yiorgos and Aristidou, Andreas and Charalambous, Panayiotis}, <br>
	year={2024}, <br>
	month=jan<br>
}

</div>